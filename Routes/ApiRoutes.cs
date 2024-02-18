#pragma warning disable 8600, 8602 // Disable null check warnings for fields that are initialized in the constructor

using Serilog;
using Gaos.Dbo;
using Gaos.Routes.Model.ApiJson;

namespace Gaos.Routes
{

    public static class ApiRoutes
    {

        public static string CLASS_NAME = typeof(ApiRoutes).Name;
        public static RouteGroupBuilder GroupApi(this RouteGroupBuilder group)
        {
            group.MapGet("/hello", (Db db) => "hello");

            group.MapGet("/tokenClaims", (HttpContext context, Db db) => 
            {
                const string METHOD_NAME = "tokenClaims";
                try 
                { 
                    Gaos.Model.Token.TokenClaims claims = context.Items[Gaos.Common.Context.HTTP_CONTEXT_KEY_TOKEN_CLAIMS] as Gaos.Model.Token.TokenClaims;
                    TokenClaimsResponse  response = new TokenClaimsResponse
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
