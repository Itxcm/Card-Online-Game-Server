﻿using AhpilyServer;
using Card_Online_Game_Server.Cache;
using Card_Online_Game_Server.Model;
using Protocol.Code;
using Protocol.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card_Online_Game_Server.Logic
{
    public class MatchHandler : IHandler
    {
        public MatchCache matchCache = Caches.MatchCache;
        public UserCache userCache = Caches.UserCache;

        public Action<List<int>> StartFightAction; // 开始战斗回调委托

        public void OnDisconnect(ClientPeer client)
        {
            UserModel userModel = userCache.GetUserModelByClient(client); // 获取当前客户端角色
            if (matchCache.IsUserHaveRoom(userModel)) Leave(client); // 角色在房间则调用离开房间
        }

        public void OnReceive(ClientPeer client, int subCode, object value)
        {
            switch (subCode)
            {
                case MatchCode.Enter_Cres:
                    Enter(client);
                    break;

                case MatchCode.Leave_Cres:
                    Leave(client);
                    break;

                case MatchCode.Ready_Cres:
                    Ready(client);
                    break;

                case MatchCode.CancleReady_Cres:
                    CancleReady(client);
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// 进入房间
        /// </summary>
        /// <param name="clientPeer"></param>
        private void Enter(ClientPeer clientPeer)
        {
            SingleExecute.Instance.Execute(() =>
            {
                if (!userCache.IsOnline(clientPeer)) return; // 角色不在线

                UserModel userModel = userCache.GetUserModelByClient(clientPeer); // 获取当前客户端角色

                if (matchCache.IsUserHaveRoom(userModel)) return; // 角色存在房间

                MatchRoom matchRoom = matchCache.Enter(userModel, clientPeer);      // 进入房间

                UserDto userDto = new UserDto
                {
                    Id = userModel.Id,
                    Avatar = userModel.Avatar,
                    AvatarMask = userModel.AvatarMask,
                    RankLogo = userModel.RankLogo,
                    RankName = userModel.RankName,
                    GradeLogo = userModel.GradeLogo,
                    GradeName = userModel.GradeName,
                    Name = userModel.Name,
                    BeanCount = userModel.BeanCount,
                    DiamondCount = userModel.DiamondCount,
                }; // 当前用户传输模型

                matchRoom.Borcast(OpCode.Match, MatchCode.Enter_Bro, userDto, clientPeer);     // 进入 广播给其他用户

                MatchRoomDto matchRoomDto = CreateMatchRoomDto(matchRoom); // 重新获取当前房间状态

                clientPeer.Send(OpCode.Match, MatchCode.Enter_Sres, matchRoomDto); // 发送房间状态
            });
        }

        /// <summary>
        /// 离开房间
        /// </summary>
        /// <param name="clientPeer"></param>
        private void Leave(ClientPeer clientPeer)
        {
            SingleExecute.Instance.Execute(() =>
            {
                if (!userCache.IsOnline(clientPeer)) return; // 角色不在线

                UserModel userModel = userCache.GetUserModelByClient(clientPeer); // 获取当前客户端角色

                if (!matchCache.IsUserHaveRoom(userModel)) return; // 当前用户不在房间

                MatchRoom matchRoom = matchCache.GetUserRoom(userModel);

                UserDto userDto = new UserDto
                {
                    Id = userModel.Id,
                    Avatar = userModel.Avatar,
                    AvatarMask = userModel.AvatarMask,
                    RankLogo = userModel.RankLogo,
                    RankName = userModel.RankName,
                    GradeLogo = userModel.GradeLogo,
                    GradeName = userModel.GradeName,
                    Name = userModel.Name,
                    BeanCount = userModel.BeanCount,
                    DiamondCount = userModel.DiamondCount,
                }; // 当前用户传输模型

                if (matchRoom.IsReady(userModel)) matchRoom.CancleReady(userModel); // 此人准备了就移除下

                matchRoom.Borcast(OpCode.Match, MatchCode.Leave_Bro, userDto); // 离开 广播给所有

                matchCache.Leave(userModel); // 离开房间
            });
        }

        /// <summary>
        ///  房间准备
        /// </summary>
        /// <param name="clientPeer"></param>
        private void Ready(ClientPeer clientPeer)
        {
            SingleExecute.Instance.Execute(() =>
            {
                if (!userCache.IsOnline(clientPeer)) return; // 角色不在线

                UserModel userModel = userCache.GetUserModelByClient(clientPeer);

                if (!matchCache.IsUserHaveRoom(userModel)) return; // 当前用户不在房间

                MatchRoom matchRoom = matchCache.GetUserRoom(userModel); // 获取房间

                if (matchRoom.IsReady(userModel)) return; // 此人已经准备了

                matchRoom.Ready(userModel); // 准备

                UserDto userDto = new UserDto
                {
                    Id = userModel.Id,
                    Avatar = userModel.Avatar,
                    AvatarMask = userModel.AvatarMask,
                    RankLogo = userModel.RankLogo,
                    RankName = userModel.RankName,
                    GradeLogo = userModel.GradeLogo,
                    GradeName = userModel.GradeName,
                    Name = userModel.Name,
                    BeanCount = userModel.BeanCount,
                    DiamondCount = userModel.DiamondCount,
                }; // 当前用户传输模型

                matchRoom.Borcast(OpCode.Match, MatchCode.Ready_Bro, userDto); // 广播给所有用户

                if (matchRoom.IsAllReady()) // 所有玩家准备
                {
                    StartFightAction(matchRoom.GetAllUserId()); // 通知服务器开始战斗了
                    matchRoom.Borcast(OpCode.Match, MatchCode.Start_Bro, null); //通知房间内其他玩家开始战斗了
                    matchCache.ClearRoom(matchRoom); // 清除房间
                }
            });
        }

        /// <summary>
        ///  取消准备
        /// </summary>
        /// <param name="clientPeer"></param>
        private void CancleReady(ClientPeer clientPeer)
        {
            SingleExecute.Instance.Execute(() =>
            {
                if (!userCache.IsOnline(clientPeer)) return; // 角色不在线

                UserModel userModel = userCache.GetUserModelByClient(clientPeer);

                if (!matchCache.IsUserHaveRoom(userModel)) return; // 当前用户不在房间

                MatchRoom matchRoom = matchCache.GetUserRoom(userModel); // 获取房间
                matchRoom.CancleReady(userModel); // 取消准备

                UserDto userDto = new UserDto
                {
                    Id = userModel.Id,
                    Avatar = userModel.Avatar,
                    AvatarMask = userModel.AvatarMask,
                    RankLogo = userModel.RankLogo,
                    RankName = userModel.RankName,
                    GradeLogo = userModel.GradeLogo,
                    GradeName = userModel.GradeName,
                    Name = userModel.Name,
                    BeanCount = userModel.BeanCount,
                    DiamondCount = userModel.DiamondCount,
                }; // 当前用户传输模型

                matchRoom.Borcast(OpCode.Match, MatchCode.CancleReady_Bro, userDto); // 广播给所有用户
            });
        }

        private MatchRoomDto CreateMatchRoomDto(MatchRoom matchRoom) // 创建传输模型 传输当前房间状态
        {
            MatchRoomDto matchRoomDto = new MatchRoomDto(); // 初始化房间传输模型

            foreach (var user in matchRoom.UserClientDic.Keys) // 遍历存储玩家信息字典
            {
                UserDto userDto = new UserDto
                {
                    Id = user.Id,
                    Avatar = user.Avatar,
                    AvatarMask = user.AvatarMask,
                    RankLogo = user.RankLogo,
                    RankName = user.RankName,
                    GradeLogo = user.GradeLogo,
                    GradeName = user.GradeName,
                    Name = user.Name,
                    BeanCount = user.BeanCount,
                    DiamondCount = user.DiamondCount,
                }; // 当前用户传输模型

                matchRoomDto.Add(userDto);
            }
            foreach (var user in matchRoom.UserReadyList)// 遍历存储准备玩家列表
            {
                matchRoomDto.readyList.Add(user.Id);
            }
            return matchRoomDto;
        }
    }
}