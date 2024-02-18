using Serilog;

namespace Gaos.WebSocket
{

    public class WebSocket
    {
        private static string CLASS_NAME = typeof(WebSocket).Name;

        public static int MAX_MESSAGE_LENGTH = 1024 * 4;

        public static List<WebSocket> Connections = new List<WebSocket>();

        public System.Net.WebSockets.WebSocket Socket;

        WebSocket(System.Net.WebSockets.WebSocket socket)
        {
            this.Socket = socket;

        }

        public static void Add(System.Net.WebSockets.WebSocket socket)
        {
            Connections.Add(new WebSocket(socket));
        }

        public static void Remove(System.Net.WebSockets.WebSocket socket)
        {
            // Remove socket from conections
            Connections.RemoveAll(c => c.Socket == socket);
        }

        public static async Task HandleMessages(System.Net.WebSockets.WebSocket socket)
        {
            const string METHOD_NAME = "HandleMessages()";

            var buffer = new byte[MAX_MESSAGE_LENGTH];

            while (socket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    Remove(socket);
                    break;
                }
                else if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Binary)
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME}: error: message in binary format received, ignoring the message ...");
                    continue;
                }
                else if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Text)
                {
                    if (result.EndOfMessage)
                    {
                        string message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await RouteMessage(socket, message);
                        continue;
                    }
                    else
                    {
                        Log.Error($"{CLASS_NAME}:{METHOD_NAME}: error: message is too long, ignoring the message ...");
                        await IgnoreMessage(socket);
                        continue;
                    }

                }
                else
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME}: error: unknow MessageType");
                    throw new Exception("unknow MessageType");
                }
            }
        }

        private static async Task IgnoreMessage(System.Net.WebSockets.WebSocket socket)
        {
            const string METHOD_NAME = "IgnoreMessage()";

            var buffer = new byte[MAX_MESSAGE_LENGTH];

            while (socket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    Remove(socket);
                    break;
                }
                else if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Binary)
                {
                    continue;
                }
                else if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Text)
                {
                    if (result.EndOfMessage)
                    {
                        break;
                        
                    }
                    else
                    {
                        continue;
                    }

                }
                else
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME}: error: unknow MessageType");
                    throw new Exception("unknow MessageType");
                }
            }

        }

        public static async Task SendMessage(System.Net.WebSockets.WebSocket socket, string message)
        {
            const string METHOD_NAME = "SendMessage()";
            try
            {
                var buffer = System.Text.Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch(Exception e)
            {
                Log.Error($"{CLASS_NAME}:{METHOD_NAME}: error: {e.Message}");
            }
        }

        public static async Task RouteMessage(System.Net.WebSockets.WebSocket socket, string message)
        {
            const string METHOD_NAME = "RouteMessage()";
            try
            {
                Log.Information($"{CLASS_NAME}:{METHOD_NAME}: @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@ received message: {message}");
                await SendMessage(socket, "pong"); //@@@@@@@@@@@@@@@@@@@@@@
            } 
            catch (Exception e)
            {
                Log.Error($"{CLASS_NAME}:{METHOD_NAME}: error: {e.Message}, message cannot be deserialized, message is ignored ......");
                return;
            }
            return;
       
        }
    }
}
