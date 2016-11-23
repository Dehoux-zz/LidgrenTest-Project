using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;

namespace LidgrenTestLobby
{
    class LobbyManager
    {
        public static NetServer Server { get; private set; }
        private NetPeerConfiguration _configuration;
        private string _approvalMessage;

        private Task _gameLoopTask;
        private volatile bool _isRunning;
        private int _beatnum;

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

            _gameLoopTask = new Task(RunGameLoop);
            _gameLoopTask.Start();
        }

        public void StopServer()
        {
            Server?.Shutdown("Stopping Server");
            _isRunning = false;
            _gameLoopTask?.Wait();
            Console.WriteLine("Lidgren Testserver was shutdown.");
        }

        #endregion

        #region GameLoop

        private async void RunGameLoop()
        {
            Console.WriteLine("Starting RunGameLoop Thread.");

            int sendRate = 1000 / 66; // 1 sec = 1000ms as Sleep uses ms.
            for (_isRunning = true; _isRunning; await Task.Delay(sendRate))
            {
                Console.Write(".");
            }
            Console.WriteLine("Stopped RunGameLoop Thread.");
        }

        #endregion

        #region ManageClients

        public Client SearchClient(NetConnection connection)
        {
            return _clients.Find(c => c.Connection == connection);
        }

        public void ManageConnectionAppovalClient(NetIncomingMessage incomingMessage)
        {
            string s = incomingMessage.ReadString();
            if (s == _approvalMessage)
            {
                Console.WriteLine("Incoming message ConnectionApproval: Approved: " + incomingMessage.SenderConnection);
                incomingMessage.SenderConnection.Approve();

                _clients.Add(new Client(_clients.Count, incomingMessage.SenderConnection, _beatnum));
            }
            else
            {
                Console.WriteLine("Incoming message ConnectionApproval: Deny: " + incomingMessage.SenderConnection);
                incomingMessage.SenderConnection.Deny();
            }
        }

        public void ManageDisonnectionClient(Client client)
        {
            Console.WriteLine("Client with client ID " + client.Id + " left the server, byebye.");

            _clients.Remove(client);
        }





        #endregion

    }
}
