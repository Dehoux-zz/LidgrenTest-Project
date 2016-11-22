using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;

namespace LidgrenTestLobby
{
    public class LobbyManager
    {
        public NetServer Server { get; private set; }
        private NetPeerConfiguration _configuration;
        private string _approvalMessage;

        private Thread _gameLoopThread;
        private volatile bool _isRunning;

        private List<Client> _clients;
        private List<Room> _rooms;

        #region Singleton Creation
        private static volatile LobbyManager _instance;
        private static object _syncRoot = new object();

        private LobbyManager() { }

        public static LobbyManager Instance
        {
            get
            {
                if (_instance != null) return _instance;
                lock (_syncRoot)
                {
                    if (_instance == null)
                        _instance = new LobbyManager();
                }

                return _instance;
            }
        }
        #endregion

        #region ServerControl
        public void InitialiseServerManager(int gamePort, int maxPlayers, string approvalMessage)
        {
            // (needed for RegisterReceivedCallback, replace with AsyncOperationM... see CrabBattleServer)
            if (SynchronizationContext.Current == null)
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            _configuration = new NetPeerConfiguration("LidgrenTest")
            {
                Port = gamePort,
                MaximumConnections = maxPlayers
            };
            _configuration.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            _approvalMessage = approvalMessage;
        }

        public void StartServer()
        {
            _clients = new List<Client>();
            _rooms = new List<Room>();

            Server = new NetServer(_configuration);
            Server.RegisterReceivedCallback(MessageHandler.Register);
            Server.Start();
            Console.WriteLine("Server Started");

            _gameLoopThread = new Thread(RunGameLoop) { Name = "Game Loop Thread" };
            _gameLoopThread.Start();
            Thread.Sleep(500);
        }

        public void StopServer()
        {
            Server?.Shutdown("Stopping Server");
            _gameLoopThread?.Join();
            Console.WriteLine("Lidgren Testserver was shutdown.");
        }
        #endregion

        #region GameLoop
        private void RunGameLoop()
        {
            Console.WriteLine("Starting RunGameLoop Thread.");

            int sendRate = 1000 / 66; // 1 sec = 1000ms as Sleep uses ms.
            for (_isRunning = true; _isRunning; Thread.Sleep(sendRate))
            {
                
            }
            Console.WriteLine("Stopped RunGameLoop Thread.");
        }
        #endregion
    }
}
