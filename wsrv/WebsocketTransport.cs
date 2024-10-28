namespace Gaos.wsrv
{
    using GaoProtobuf;
    using Google.Protobuf;
    using System;
    using System.IO;

    public class WebsocketTransport
    {

        public static byte[] SerializeMessageHeader(GaoProtobuf.MessageHeader messageHeader)
        {
            byte[] message = messageHeader.ToByteString().ToByteArray();
            byte[] size = BitConverter.GetBytes(ToNetworkByteOrder((uint)message.Length));
            byte[] buffer = new byte[size.Length + message.Length];
            size.CopyTo(buffer, 0);
            message.CopyTo(buffer, size.Length);
            return buffer;
        }


        public static byte[] SerializeMessage(GaoProtobuf.MessageHeader messageHeader, byte [] message)
        {
            byte[] header = SerializeMessageHeader(messageHeader);
            byte[] sizeMessage = BitConverter.GetBytes(ToNetworkByteOrder((uint)message.Length));
            byte[] sizeTotal = BitConverter.GetBytes(ToNetworkByteOrder((uint)header.Length + (uint)sizeMessage.Length + (uint)message.Length));


            // concatente total size, header, size of message, and message
            byte[] buffer = new byte[sizeTotal.Length + header.Length + sizeMessage.Length + message.Length];
            sizeTotal.CopyTo(buffer, 0);
            header.CopyTo(buffer, sizeTotal.Length);
            sizeMessage.CopyTo(buffer, sizeTotal.Length + header.Length);
            message.CopyTo(buffer, sizeTotal.Length + header.Length + sizeMessage.Length);

            return buffer;
        }

        public static uint ToNetworkByteOrder(uint value)
        {
            // to big endian
            return ((value & 0x000000FF) << 24) |
                   ((value & 0x0000FF00) << 8) |
                   ((value & 0x00FF0000) >> 8) |
                   ((value & 0xFF000000) >> 24);
        }
    }
}
