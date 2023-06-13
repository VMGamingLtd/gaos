﻿#pragma warning disable 8600, 8602 

using Serilog;
using Gaos.Auth;

namespace Gaos.Middleware
{
    public class AuthMiddleware
    {
        public static string CLASS_NAME = typeof(AuthMiddleware).Name;

        public static string HTTP_CONTEXT_KEY_TOKEN_CLAIMS = "token_claims";

        private readonly RequestDelegate _next;

        private string? ExtractBrearerToken(string authHeader)
        {
            string[] parts = authHeader.Split(' ');
            if (parts.Length != 2)
            {
                return null;
            }
            if (parts[0] != "Bearer")
            {
                return null;
            }
            return parts[1];
        }


        public AuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, Token token)
        {
            const string METHOD_NAME = "Invoke()";

            string path = context.Request.Path.Value;

             

            if (path != null && path.StartsWith("/api"))
            {

                string? authHeader = context.Request.Headers["Authorization"];

                if (authHeader == null)
                {
                    Log.Warning($"{CLASS_NAME}:{METHOD_NAME} {path} - Unauthorized: missing Authorization header");
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }

                string? jwt = ExtractBrearerToken(authHeader);
                if (jwt == null)
                {
                    Log.Warning($"{CLASS_NAME}:{METHOD_NAME} {path} - Unauthorized: no bearer token in Authorization header");
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }

                Gaos.Model.Token.TokenClaims claims = token.GetClaimsFormJWT(jwt);

                if (claims == null)
                {
                    Log.Warning($"{CLASS_NAME}:{METHOD_NAME} {path} - Unauthorized: could not decode claims");
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }

                long secondsNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (secondsNow > claims.Exp)
                {
                    Log.Warning($"{CLASS_NAME}:{METHOD_NAME} {path} - Unauthorized: token expired");
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }

                context.Items.Add(HTTP_CONTEXT_KEY_TOKEN_CLAIMS, claims);


                await _next(context);
            }
            else
            {
                await _next(context);
            }
        }
    }

    public static class AuthMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthMiddleware>();
        }
    }

}
