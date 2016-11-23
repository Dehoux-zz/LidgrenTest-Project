using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace LidgrenTestLobby
{
    class Player : Client
    {
        public Player(int id, NetConnection connection, int curBeat) : base(id, connection, curBeat)
        {
            
        }
    }
}
