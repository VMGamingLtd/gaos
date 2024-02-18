#pragma warning disable 8600, 8602 

using Serilog;
using Gaos.WebSocket;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

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

        public async Task Invoke(HttpContext context, Dbo.Db db)
        {
            int sessionId;
            // Check if the "MyCookie" is not already present in the request
            if (!context.Request.Cookies.ContainsKey(SESSION_COOKIE_NAME))
            {
                sessionId = await CreateNewCookie(context, db);
            }
            else
            {
                try 
                {
                    sessionId = ReadCookieValue(context);
                    // Verify if sessionId exists in Session table
                    Dbo.Model.Session session = await db.Session.FirstOrDefaultAsync(s => s.Id == sessionId);
                    if (session == null)
                    {
                        sessionId = await CreateNewCookie(context, db);
                    }
                    else
                    {
                        // Update session
                        session.AccessedAt = DateTime.Now;
                        await db.SaveChangesAsync();
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, $"{CLASS_NAME}: {e.Message}");
                    Log.Error($"{CLASS_NAME}: will create new cookie");
                    sessionId = await CreateNewCookie(context, db);
                }

            }

            context.Items.Add(Gaos.Common.Context.HTTP_CONTEXT_KEY_SESSION_ID, sessionId);

            // Call the next middleware in the pipeline
            await _next(context);
        }

        private async Task<int> CreateNewCookie(HttpContext context, Dbo.Db db)
        {
            // Add entry ib Db Session table
            Dbo.Model.Session session = new Dbo.Model.Session();
            var now = DateTime.UtcNow;
            session.CreatedAt = DateTime.UtcNow;
            db.Session.Add(session);
            await db.SaveChangesAsync();

            int sessionId = session.Id;
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
