using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;

namespace LidgrenTestLobby
{
    class Client
    {
        public int Id { get; }
        public NetConnection Connection { get; }
        public int LastBeat;
        public double KeepAlive = 0, LastKeepAlive = 0;

        public Client(int id, NetConnection connection, int curBeat)
        {
            Id = id;
            Connection = connection;
            LastBeat = curBeat;

            AssignIdToClient();
        }
        
        /// <summary>
        /// Let the Client know he has been accepted by the server and has been given an ID.
        /// </summary>
        private async void AssignIdToClient()
        {
            //Wait a moment before sending to client, client may not be ready yet <--- tricky code
            await Task.Delay(2000);

            NetOutgoingMessage outgoingMessage = LobbyManager.Server.CreateMessage();
            outgoingMessage.Write((byte)PacketTypes.AssignId);
            outgoingMessage.Write(Id);
            LobbyManager.Server.SendMessage(outgoingMessage, Connection, NetDeliveryMethod.ReliableOrdered, 1);

            outgoingMessage = LobbyManager.Server.CreateMessage();
            outgoingMessage.Write((byte)PacketTypes.Message);
            outgoingMessage.Write("You (Client: " + Id + ") now got an ID, welcome on the Server.");
            LobbyManager.Server.SendMessage(outgoingMessage, Connection, NetDeliveryMethod.ReliableOrdered, 0);
        }

        public void StatusChange(NetIncomingMessage incomingMessage)
        {
            NetConnectionStatus status = (NetConnectionStatus)incomingMessage.ReadByte();
            string reason = incomingMessage.ReadString();

            if (Connection.Status == NetConnectionStatus.Disconnected || Connection.Status == NetConnectionStatus.Disconnecting)
            {
                LobbyManager.Instance.ManageDisonnectionClient(this);
            }

            Console.WriteLine("Client id " + Id + "; status changed to " + status + " (" + reason + ") " + ".");

        }
    }
}
