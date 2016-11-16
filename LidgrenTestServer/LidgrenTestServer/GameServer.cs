using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using System.Threading;

namespace LidgrenTestServer
{
    public sealed class GameServer
    {
        private static volatile GameServer _instance;
        private static object _syncRoot = new Object();

        private GameServer() { }

        public static GameServer Instance
        {
            get
            {
                if (_instance != null) return _instance;
                lock (_syncRoot)
                {
                    if (_instance == null)
                        _instance = new GameServer();
                }

                return _instance;
            }
        }

        private NetPeerConfiguration _config;
        private static NetServer _server;
        private static string _approvalMessage;

        public void SetGameServerSettings(int gamePort, int maxPlayers, string approvalMessage)
        {
            // (needed for RegisterReceivedCallback, TODO replace with AsyncOperationM... see CrabBattleServer)
            if (SynchronizationContext.Current == null)
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            _config = new NetPeerConfiguration("LidgrenTest")
            {
                Port = gamePort,
                MaximumConnections = maxPlayers
            };
            _config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            _approvalMessage = approvalMessage;
        }

        public void Start()
        {
            _server = new NetServer(_config);
            _server.Start();
        }

        public void Stop()
        {
            
        }

    }

}
