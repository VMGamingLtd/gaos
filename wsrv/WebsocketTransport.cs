namespace Gaos.wsrv
{
    using GaoProtobuf;
    using System;
    using System.IO;

    public class WebsocketTransport
    {
        public static void SerializeMessage<T>(Stream stream, MessageHeader messageHeader, T message) where T : Google.Protobuf.IMessage
        {
            using (Google.Protobuf.CodedOutputStream codedStream = new Google.Protobuf.CodedOutputStream(stream, true))
            {
                // Serialize the message header
                codedStream.WriteUInt32(ToNetworkByteOrder((uint)messageHeader.CalculateSize()));
                messageHeader.WriteTo(codedStream);
                
                // Serialize the actual message
                codedStream.WriteUInt32(ToNetworkByteOrder((uint)message.CalculateSize()));
                message.WriteTo(codedStream);
            }
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
