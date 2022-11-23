﻿using AhpilyServer;
using Card_Online_Game_Server.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Protocol.Code;

namespace Card_Online_Game_Server
{
    /// <summary>
    /// 网络消息中心
    /// </summary>
    public class NetMsgCenter : IApplication
    {
        private IHandler account = new AccountHandler();
        private IHandler user = new UserHandler();

        public void OnDisconnect(ClientPeer client)
        {
            account.OnDisconnect(client);
            user.OnDisconnect(client);
        }

        public void OnReceive(ClientPeer client, SocketMsg msg)
        {
            switch (msg.OpCode)
            {
                case OpCode.ACCOUNT:
                    account.OnReceive(client, msg.SubCode, msg.Value);
                    break;

                case OpCode.User:
                    user.OnReceive(client, msg.SubCode, msg.Value);
                    break;

                default:
                    break;
            }
        }
    }
}