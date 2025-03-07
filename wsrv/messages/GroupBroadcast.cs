﻿namespace Gaos.wsrv.messages
{
    using GaoProtobuf;
    using GaoProtobuf.gaos;
    using Gaos.wsrv;
    using System;
    using System.Threading.Tasks;
    using Serilog;
    using Google.Protobuf;

    public class GroupBroadcastService
    {
        private static readonly string CLASS_NAME = typeof(GroupBroadcastService).Name;

        private readonly WsrConnectionPoolService _connectionPool;

        public GroupBroadcastService(WsrConnectionPoolService connectionPool)
        {
            _connectionPool = connectionPool;
        }

        public async Task<bool> BroadcastCreditsChangeAsync(int fromUserId, int groupId, float credits)
        {
            const string METHOD_NAME = "BroadcastCreditsChangeAsync()";
            try
            {
                // Create the MessageHeader
                MessageHeader messageHeader = new MessageHeader
                {
                    FromId = fromUserId,
                    ToId = 0, 
                    GroupId = groupId,
                    TypeId = 0, 
                    NamespaceId = (int)NamespaceIds.Gaos,
                    ClassId = (int)GaosClassIds.Broadcast,
                    MethodId = (int)GaosBroadcastMethodIds.GroupCreditsChange
                };

                // Create the GroupCreditsChange message
                GroupCreditsChange creditsChange = new GroupCreditsChange
                {
                    Credits = credits
                };

                // Serialize the complete message using WebsocketTransport
                byte[] messageBytes = WebsocketTransport.SerializeMessage(messageHeader, creditsChange.ToByteArray());

                // Send the data asynchronously using the connection pool
                bool wasSent = await _connectionPool.SendDataAsync(messageBytes);
                if (!wasSent)
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME}: error: failed to send data");
                }
                return wasSent;

            }
            catch (Exception ex)
            {
                Log.Error($"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                return false;
            }
        }
    }
}
