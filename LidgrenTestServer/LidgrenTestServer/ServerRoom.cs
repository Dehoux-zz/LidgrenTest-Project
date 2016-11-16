using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LidgrenTestServer
{
    public class ServerRoom
    {
        public string ServerRoomId { get; set; }
        public List<Player> Players { get; set; }

        public ServerRoom(string serverRoomId)
        {
            ServerRoomId = serverRoomId;
            Players = new List<Player>();
        }
    }
}
