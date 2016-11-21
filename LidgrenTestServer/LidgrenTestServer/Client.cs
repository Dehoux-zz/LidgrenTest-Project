using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace LidgrenTestServer
{
    public class Client
    {
        public int Id { get; }
        public NetConnection Connection { get; }
        public int LastBeat;
        public double KeepAlive = 0, LastKeepAlive = 0;
        public ServerRoom joinedRoom;

        protected NetOutgoingMessage _outgoingMessage;
        protected ServerManager serverManager = ServerManager.Instance;


        public Client(int id, NetConnection connection, int curBeat)
        {
            Id = id;
            Connection = connection;
            LastBeat = curBeat;
        }


    }
}
