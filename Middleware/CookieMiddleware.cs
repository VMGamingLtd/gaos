#pragma warning disable 8600, 8602 

using Serilog;
using Gaos.WebSocket;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Gaos.Middleware
{
    public class CookieMiddleware
    {
        public static string CLASS_NAME = typeof(CookieMiddleware).Name;

        public static string SESSION_COOKIE_NAME = "vmgaming";

        private readonly RequestDelegate _next;
        private readonly IDataProtector _dataProtector;

        public CookieMiddleware(RequestDelegate next, IDataProtectionProvider dataProtectionProvider)
        {
            _next = next;
            _dataProtector = dataProtectionProvider.CreateProtector("CookieForSession"); // Use a unique purpose string
        }

        public async Task Invoke(HttpContext context, MySqlDataSource dataSource)
        {
            int sessionId;
            // Check if the "MyCookie" is not already present in the request
            if (!context.Request.Cookies.ContainsKey(SESSION_COOKIE_NAME))
            {
                sessionId = await CreateNewCookie(context, dataSource);
            }
            else
            {
                try 
                {
                    sessionId = ReadCookieValue(context);
                    bool sessionExists = await SessionExists(sessionId, dataSource);
                    if (!sessionExists)
                    {
                        sessionId = await CreateNewCookie(context, dataSource);
                    }
                    else
                    {
                        await UpdateSession(sessionId, dataSource);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, $"{CLASS_NAME}: {e.Message}");
                    Log.Error($"{CLASS_NAME}: will create new cookie");
                    sessionId = await CreateNewCookie(context, dataSource);
                }

            }

            context.Items.Add(Gaos.Common.Context.HTTP_CONTEXT_KEY_SESSION_ID, sessionId);

            // Call the next middleware in the pipeline
            await _next(context);
        }

        private async Task<bool> SessionExists(int sessionId, MySqlDataSource dataSource)
        {
            const string METHOD_NAME = "SessionExists()";
            try
            {
                using var connection = await dataSource.OpenConnectionAsync();
                using var command = connection.CreateCommand();

                string sql = "SELECT COUNT(*) FROM Session WHERE Id = @Id";
                command.CommandText = sql;
                command.Parameters.AddWithValue("@Id", sessionId);

                var result = await command.ExecuteScalarAsync();
                if (result == null)
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME}: Could not check if session exists: result is null");
                    throw new Exception("Could not check if session exists: result is null");
                }
                return (long)result > 0;
            }
            catch (Exception e)
            {
                Log.Error(e, $"{CLASS_NAME}:{METHOD_NAME}: {e.Message}");
                throw new Exception("Could not check if session exists");
            }
        }

        private async Task UpdateSession(int sessionId, MySqlDataSource dataSource)
        {
            const string METHOD_NAME = "UpdateSession()";
            try
            {
                using var connection = await dataSource.OpenConnectionAsync();
                using var command = connection.CreateCommand();

                string sql = "UPDATE Session SET AccessedAt = NOW() WHERE Id = @Id";
                command.CommandText = sql;
                command.Parameters.AddWithValue("@Id", sessionId);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, $"{CLASS_NAME}:{METHOD_NAME}: {e.Message}");
                throw new Exception("Could not update session");
            }
        }

        private async Task<int> CreateNewCookie(HttpContext context, MySqlDataSource dataSource)
        {
            int sessionId;
            try 
            {
                using var connection = await dataSource.OpenConnectionAsync();
                using var command = connection.CreateCommand();

                string sql = @"
                    INSERT INTO Session (CreatedAt, ExpiresdAt, AccessedAt) 
                    VALUES (NOW(), DATE_ADD(NOW(), INTERVAL 1000 YEAR), NOW());
                    SELECT LAST_INSERT_ID();";
                command.CommandText = sql;

                var result = await command.ExecuteScalarAsync();
                if (result == null)
                {
                    Log.Error($"{CLASS_NAME}: Could not create new session: result is null");
                    throw new Exception("Could not create new session: result is null");
                }
                if (!int.TryParse(result.ToString(), out sessionId))
                {
                    Log.Error($"{CLASS_NAME}: Could not create new session: could not parse result");
                    throw new Exception("Could not create new session: could not parse result");
                }

            }
            catch (Exception e)
            {
                Log.Error(e, $"{CLASS_NAME}: {e.Message}");
                throw new Exception("Could not create new session");
            }

            string sessionIdString = sessionId.ToString();

            var sessionIdEncrypted = _dataProtector.Protect(sessionIdString);

            var cookieOptions = new CookieOptions
            {
                Path = "/",
                Secure = true,
                HttpOnly = true
            };
            context.Response.Cookies.Append(SESSION_COOKIE_NAME, sessionIdEncrypted, cookieOptions);

            return sessionId;

        }

        private int ReadCookieValue(HttpContext context)
        {
            var protectedValue = context.Request.Cookies[SESSION_COOKIE_NAME];
            if (protectedValue == null)
            {
                throw new Exception("Cookie not found");
            }
            var value = _dataProtector.Unprotect(protectedValue);
            return int.Parse(value);
        }


    }

    public static class CookieMiddlewareExtensions
    {
        public static IApplicationBuilder UseCookieMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CookieMiddleware>();
        }
    }

}
