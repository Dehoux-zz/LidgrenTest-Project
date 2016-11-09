using System;
using System.Collections.Generic;
using Lidgren.Network;

public static class NetworkMessages
{
    public static void SendPlayerUpdate(int id, float xPos, float yPos)
    {
        NetOutgoingMessage netOutgoingMessage = ServerConnection.CreateNetOutgoingMessage();
        netOutgoingMessage.Write((byte)PackageTypes.PlayerMovement);
        netOutgoingMessage.Write(id);
        netOutgoingMessage.Write(xPos);
        netOutgoingMessage.Write(yPos);
        ServerConnection.SendNetOutgoingMessage(netOutgoingMessage, NetDeliveryMethod.ReliableOrdered, 2);
    }





}

//[Serializable]
//public abstract class LidgrenMessage
//{
//    public byte[] ToByteArray()
//    {
//        BinaryFormatter bf = new BinaryFormatter();
//        using (MemoryStream ms = new MemoryStream())
//        {
//            bf.Serialize(ms, this);
//            return ms.ToArray();
//        }
//    }
//}

//[Serializable]
//public class UpdatePositionMessage
//{
//    public float xPos, yPos;

//    public static UpdatePositionMessage FromByteArray(byte[] byteArray)
//    {
//        BinaryFormatter bf = new BinaryFormatter();
//        using (MemoryStream ms = new MemoryStream(byteArray))
//        {
//            try
//            {
//                return bf.Deserialize(ms) as UpdatePositionMessage;
//            }
//            catch (Exception e)
//            {
//                //Debug.Log(e);
//                return null;
//            }
//        }
//    }

//}