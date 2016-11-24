using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace LidgrenTestLobby
{
    class Player
    {
        public Client Client;

        public Player(Client client)
        {
            Client = client;
        }
    }
}
