namespace Gaos.wsrv.messages
{
    using GaoProtobuf;
    using GaoProtobuf.gaos;
    using Gaos.wsrv;
    using System;
    using System.Threading.Tasks;
    using Serilog;

    public class GroupBroadcastService
    {
        private static readonly string CLASS_NAME = typeof(GroupBroadcastService).Name;

        private readonly WsrConnectionPoolService _connectionPool;

        public GroupBroadcastService(WsrConnectionPoolService connectionPool)
        {
            _connectionPool = connectionPool;
        }

        public async Task BroadcastCreditsChangeAsync(int fromUserId, int groupId, float credits)
        {
            const string METHOD_NAME = "BroadcastCreditsChangeAsync()";
            try
            {
                // Ensure the connection pool is initialized
                await _connectionPool.Init();

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
                byte[] messageBytes = WebsocketTransport.SerializeMessage(messageHeader, creditsChange);

                // Send the data asynchronously using the connection pool
                await _connectionPool.SendDataAsync(messageBytes);

            }
            catch (Exception ex)
            {
                Log.Error($"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
            }
        }
    }
}
