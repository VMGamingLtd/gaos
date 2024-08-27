#pragma warning disable 8600, 8602 // Disable null check warnings for fields that are initialized in the constructor

using gaos.Routes.Model.Api1;
using Gaos.Dbo;
using Gaos.Routes.Model.ApiJson;
using Gaos.Routes.Model.GameDataJson;
using Serilog;

namespace Gaos.Routes
{

    public static class Api1Routes
    {

        public static string CLASS_NAME = typeof(Api1Routes).Name;
        public static RouteGroupBuilder GroupApi1(this RouteGroupBuilder group)
        {
            group.MapGet("/hello", (Db db) =>
            {
                var hello1 = new HelloResponse();
                hello1.Msg = "Hello";
                return Results.Json(hello1);
            });

            group.MapGet("/userGameDataGet", async (int userId, int slotId, Db db, Gaos.Common.UserService userService, Gaos.Mongo.GameData gameDataService) =>
            {
                const string METHOD_NAME = "userGameDataGet()";
                try
                {
                    UserGameDataGetResponse response;

                    var result = await gameDataService.GetGameDataAsync(userId, slotId);

                    if (result.IsError)
                    {
                        response = new UserGameDataGetResponse
                        {
                            IsError = true,
                            ErrorMessage = result.ErrorMessage,
                        };
                        return Results.Json(response);
                    }

                    response = new UserGameDataGetResponse
                    {
                        IsError = false,
                        ErrorMessage = "",
                        Id = result.Id,
                        Version = result.Version,
                        GameDataJson = result.GameDataJson,
                    };

                    return Results.Json(response);

                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    UserGameDataGetResponse response = new UserGameDataGetResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error",
                    };
                    return Results.Json(response);
                }

            });

            group.MapGet("/tokenClaims", (HttpContext context, Db db) =>
            {
                const string METHOD_NAME = "tokenClaims";
                try
                {
                    Gaos.Model.Token.TokenClaims claims = context.Items[Gaos.Common.Context.HTTP_CONTEXT_KEY_TOKEN_CLAIMS] as Gaos.Model.Token.TokenClaims;
                    TokenClaimsResponse response = new TokenClaimsResponse
                    {
                        IsError = false,
                        TokenClaims = claims,

                    };
                    return Results.Json(response);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    TokenClaimsResponse response = new TokenClaimsResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error",
                    };
                    return Results.Json(response);
                }

            });


            return group;

        }
    }
}
