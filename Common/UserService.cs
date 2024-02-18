#pragma warning disable 8625, 8603, 8629, 8604

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog;
namespace Gaos.Common
{
    public class UserService
    {
        private static string CLASS_NAME = typeof(UserService).Name;
        private Gaos.Auth.TokenService TokenService= null;
        private HttpContext Context = null;
        private  Gaos.Dbo.Db Db = null;

        private Gaos.Dbo.Model.User? User = null;

        public UserService(HttpContext context, Auth.TokenService tokenService, Gaos.Dbo.Db db)
        {
            TokenService = tokenService;
            Context = context;
            Db = db;
        }

        public Gaos.Model.Token.TokenClaims GetTokenClaims()
        {
            const string METHOD_NAME = "GetTokenClaims()";
            Gaos.Model.Token.TokenClaims? claims;
            if (Context.Items.ContainsKey(Gaos.Common.Context.HTTP_CONTEXT_KEY_TOKEN_CLAIMS) == false)
            {
                Log.Error($"{CLASS_NAME}:{METHOD_NAME} no token claims");
                throw new Exception("no token claims");
            }
            else
            {
                claims = Context.Items[Gaos.Common.Context.HTTP_CONTEXT_KEY_TOKEN_CLAIMS] as Gaos.Model.Token.TokenClaims;
            }
            return claims;
        }

        public int GetUserId()
        {
            Gaos.Model.Token.TokenClaims claims = GetTokenClaims();
            return claims.UserId;

        }

        public Gaos.Dbo.Model.User GetUser()
        {
            const string METHOD_NAME = "GetUser()";
            if (User == null)
            {
                int userId = GetUserId();
                User = Db.User.FirstOrDefault(x => x.Id == userId);
                if (User == null)
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME} no such user");
                    throw new Exception("no such user");
                    
                }
                return User;
            }
            else
            {
                return User;
            }

        }

        public (Gaos.Dbo.Model.User?, Gaos.Dbo.Model.JWT?) GetDeviceUser(int deviceId)
        {
            const string METHOD_NAME = "GetDeviceUser()";
            Gaos.Dbo.Model.User? user = null;

            Gaos.Dbo.Model.JWT? jwt = Db.JWT.FirstOrDefault(x => x.DeviceId == deviceId);
            if (jwt != null) {
                user = Db.User.FirstOrDefault(x => x.Id == jwt.UserId);
                if (user == null)
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME} user not found for token");
                    throw new Exception("user not found for token");
                }
                var userType = (bool)user.IsGuest ? Gaos.Model.Token.UserType.GuestUser : Gaos.Model.Token.UserType.RegisteredUser;
                var jwtStr = TokenService.GenerateJWT(
                    user.Name, user.Id, deviceId, 
                    DateTimeOffset.UtcNow.AddHours(Gaos.Common.Context.TOKEN_EXPIRATION_HOURS).ToUnixTimeSeconds(), 
                    userType);
                jwt = Db.JWT.FirstOrDefault(x => x.DeviceId == deviceId);
                if (jwt != null)
                {
                    return (user, jwt);
                }
                else
                {
                    Log.Error($"{CLASS_NAME}:GetDeviceUser() no jwt");
                    throw new Exception("no jwt");
                }
            }
            else
            {
                return (user, jwt);
            }

        }
    }
}
