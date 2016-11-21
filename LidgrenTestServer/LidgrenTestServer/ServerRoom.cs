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
        public List<Client> Players { get; set; }
        private ServerManager _serverManager;

        public ServerRoom(int id)
        {
            Id = id;
            Name = "Lidgren Room: " + id;
            Players = new List<Client>();
            _serverManager = ServerManager.Instance;
        }

        public void AddNewPlayer(Client newClient)
        {
            Players.Add(newClient);
            //Send all otherPlayers to new player excl. himself
            foreach (Client otherPlayer in Players.Where(otherPlayer => otherPlayer.Id != newClient.Id))
            {
                _serverManager.SendPlayerToPlayer(newClient, otherPlayer);
            }
        }
    }
}
