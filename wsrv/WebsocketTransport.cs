namespace Gaos.wsrv
{
    using GaoProtobuf;
    using System;
    using System.IO;

    public class WebsocketTransport
    {
        public static byte[] SerializeMessage<T>(MessageHeader messageHeader, T message) where T : Google.Protobuf.IMessage
        {
            byte[] messageBytes;
            using (var stream = new System.IO.MemoryStream())
            {
                using (Google.Protobuf.CodedOutputStream codedStream = new Google.Protobuf.CodedOutputStream(stream, true))
                {
                    // Serialize the message header
                    codedStream.WriteUInt32(ToNetworkByteOrder((uint)messageHeader.CalculateSize()));
                    messageHeader.WriteTo(codedStream);

                    // Serialize the actual message
                    codedStream.WriteUInt32(ToNetworkByteOrder((uint)message.CalculateSize()));
                    message.WriteTo(codedStream);

                    messageBytes = stream.ToArray();
                }
            }

            using (var stream = new System.IO.MemoryStream())
            {
                int messageLength = messageBytes.Length;
                stream.Write(BitConverter.GetBytes(ToNetworkByteOrder((uint)messageLength)), 0, 4);
                stream.Write(messageBytes, 0, messageLength);
                messageBytes = stream.ToArray();
            }

            return messageBytes;
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
