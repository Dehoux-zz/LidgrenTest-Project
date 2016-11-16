using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using UnityEngine;

namespace LidgrenTestServer
{
    /// <summary>
    /// Character class
    /// 
    /// This class is passed around.
    /// It holds the position, name ( not used in this example ) ( even thou it gets sent all over )
    /// Connection (ip+port)
    /// 
    /// </summary>
    public class Player
    {
        public int Id { get; }
        public string Name { get; }
        public NetConnection Connection { get; }
        public Vector2 Position;
        public Vector2 Velocity;
        public int LastBeat;
        public double KeepAlive = 0, LastKeepAlive = 0;
        public bool Grounded;

        private NetOutgoingMessage _outgoingMessage;
        private ServerManager serverManager = ServerManager.Instance;


        public Player(int id, NetConnection connection, string name, int curBeat)
        {
            Id = id;
            Connection = connection;
            Name = name;
            Position = new Vector2();
            LastBeat = curBeat;
            Velocity = Vector2.zero;
            Grounded = false;
        }

        public void HandlePlayerMovement(NetIncomingMessage incomingMessage)
        {
            Position = incomingMessage.ReadVector2();
            Velocity = incomingMessage.ReadVector2();
            Grounded = incomingMessage.ReadBoolean();

            Console.WriteLine("Id: " + Id + " Position: " + Position + " Velocity: " + Velocity + " Grounded: " + Grounded);

            _outgoingMessage = serverManager.Server.CreateMessage();
            _outgoingMessage.Write((byte)PacketTypes.PlayerMovement);
            _outgoingMessage.Write((Int16)Id);
            _outgoingMessage.Write(Position);
            _outgoingMessage.Write(Velocity);
            _outgoingMessage.Write(Grounded);
            //outgoingMessage.Write(player.Connection.AverageRoundtripTime / 2f);
            serverManager.Server.SendToAll(_outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.UnreliableSequenced, 10);
        }

        public void HandlePlayerJump(NetIncomingMessage incomingMessage)
        {
            _outgoingMessage = serverManager.Server.CreateMessage();
            _outgoingMessage.Write((byte)PacketTypes.PlayerJump);
            _outgoingMessage.Write((Int16)Id);
            serverManager.Server.SendToAll(_outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableUnordered, 11);
        }

        /// <summary>
        /// Send player information to other players
        /// </summary>
        /// <param name="incomingMessage"></param>
        public void SentPlayerToOthers(NetIncomingMessage incomingMessage)
        {
            Console.WriteLine("Send to all player");
            _outgoingMessage = serverManager.Server.CreateMessage();
            _outgoingMessage.Write((byte)PacketTypes.AddPlayer);
            _outgoingMessage.Write(Id);
            _outgoingMessage.Write(Name);
            _outgoingMessage.Write(Position);
            serverManager.Server.SendToAll(_outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 7);
        }
    }
}
