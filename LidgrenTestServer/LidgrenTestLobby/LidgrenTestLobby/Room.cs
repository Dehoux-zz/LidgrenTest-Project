using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LidgrenTestLobby
{
    class Room
    {
        public int Id;
        public List<Player> Players;
        public string Name = "TestRoom";
        public List<string> RoomConsole; 


        private bool _roomAlive;
        private Task _roomLoopTask;

        public Room()
        {
            Players = new List<Player>();
            RoomConsole = new List<string>();

            _roomLoopTask = new Task(RunRoomLoop);
            _roomLoopTask.Start();
        }

        public void DestroyRoom()
        {
            _roomAlive = false;
            _roomLoopTask?.Wait();
        }

        private async void RunRoomLoop()
        {
            int sendRate = 1000 / 66; // 1 sec = 1000ms as Sleep uses ms.
            for (_roomAlive = true; _roomAlive; await Task.Delay(sendRate))
            {

            }
        }

        public void PlayerEntersRoom(Player player)
        {
            Players.Add(player);

        }

        public void ConsoleWrite(string message)
        {
            RoomConsole.Add(message);
        }
    }
}
