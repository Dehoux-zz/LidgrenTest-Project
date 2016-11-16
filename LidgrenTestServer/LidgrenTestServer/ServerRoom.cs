using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LidgrenTestServer
{
    public class ServerRoom
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Player> Players { get; set; }
        private ServerManager _serverManager;

        public ServerRoom(int id)
        {
            Id = id;
            Name = "Lidgren Room: " + id;
            Players = new List<Player>();
            _serverManager = ServerManager.Instance;
        }

        public void AddNewPlayer(Player newPlayer)
        {
            Players.Add(newPlayer);

            //Send all otherPlayers to new player excl. himself
            foreach (Player otherPlayer in Players.Where(otherPlayer => otherPlayer.Id != newPlayer.Id))
            {
                _serverManager.SendPlayerToPlayer(newPlayer, otherPlayer);
            }
        }
    }
}
