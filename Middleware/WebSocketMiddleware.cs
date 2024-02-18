#pragma warning disable 8600, 8602 

using Serilog;
using Gaos.WebSocket;

namespace Gaos.Middleware
{
    public class WebSocketMiddleware
    {
        public static string CLASS_NAME = typeof(WebSocketMiddleware).Name;

        public static string HTTP_CONTEXT_KEY_TOKEN_CLAIMS = "token_claims";

        private readonly RequestDelegate _next;

        public WebSocketMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            string path = context.Request.Path.Value;

            if (path == null || !path.StartsWith("/ws"))
            {
                await _next(context);
                return;
            }

            if (context.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                Gaos.WebSocket.WebSocket.Add(webSocket);
                await Gaos.WebSocket.WebSocket.HandleMessages(webSocket);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

    }

    public static class WebSocketMiddlewareExtensions
    {
        public static IApplicationBuilder UseWebSocketMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<WebSocketMiddleware>();
        }
    }

}
