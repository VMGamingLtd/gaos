#pragma warning disable 8600, 8602, 8604, 8714
using Gaos.Dbo;
using Gaos.Routes.Model.GameDataJson;
using Gaos.Routes.Model.GroupDataJson;
using Serilog;

namespace Gaos.Routes
{
    public static class GroupDataRoutes
    {

        public static string CLASS_NAME = typeof(GroupDataRoutes).Name;
        public static RouteGroupBuilder GroupGroupData(this RouteGroupBuilder group)
        {

            group.MapPost("/getGroupData", async (GetGroupDataGetRequest request, Db db, Gaos.Common.UserService userService, Gaos.Mongo.GameData gameDataService) =>
            {
                const string METHOD_NAME = "getGroupData()";
                try
                {
                    GetGroupDataResponse response = new GetGroupDataResponse();


                    response.IsError = true;
                    response.ErrorMessage = "not implemented";
                    return Results.Json(response);

                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    GetGroupDataResponse response = new GetGroupDataResponse
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
