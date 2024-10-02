#pragma warning disable 8600, 8602, 8604, 8714
using Gaos.Mongo;
using Gaos.Dbo;
using Gaos.Routes.Model.GameDataJson;
using Gaos.Routes.Model.GroupDataJson;
using Serilog;
using static Gaos.Common.UserService;
using System.Diagnostics;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace Gaos.Routes
{
    public static class GroupDataRoutes
    {

        public static string CLASS_NAME = typeof(GroupDataRoutes).Name;
        public static RouteGroupBuilder GroupData(this RouteGroupBuilder group)
        {

            group.MapPost("/getGroupData", async (GetGroupDataGetRequest request, Db db, Gaos.Common.UserService userService, 
                GroupData groupDataService, IConfiguration configuration) =>
            {
                const string METHOD_NAME = "getGroupData()";
                try
                {
                    Stopwatch stopwatch = new Stopwatch();

                    GetGroupDataResponse response = new GetGroupDataResponse();

                    if (configuration["gao_profiling"] == "true")
                    {
                        stopwatch.Start();
                    }
                    GetGroupResult userGroup = await userService.GetUserGroup();
                    if (configuration["gao_profiling"] == "true")
                    {
                        stopwatch.Stop();
                        Log.Debug($"{CLASS_NAME}:{METHOD_NAME}: GetUserGroup() took {stopwatch.ElapsedMilliseconds} ms");
                    }
                    if (userGroup == null)
                    {
                        response.IsError = true;
                        response.ErrorMessage = "user not in group";
                        return Results.Json(response);
                    }

                    //public async Task<GetGameDataResult> GetGameData(Groupp group, int slotId = 1)
                    if (configuration["gao_profiling"] == "true")
                    {
                        stopwatch.Start();
                    }
                    GetGameDataResult gameDataResult = await groupDataService.GetGameData(userGroup, request.Version, request.IsGameDataDiff, request.SlotId);
                    if (configuration["gao_profiling"] == "true")
                    {
                        stopwatch.Stop();
                        Log.Debug($"{CLASS_NAME}:{METHOD_NAME}: GetGameData() took {stopwatch.ElapsedMilliseconds} ms");
                    }



                    response.IsError = false;
                    response.ErrorMessage = "";
                    response.Id = gameDataResult.Id;
                    response.Version = gameDataResult.Version;
                    response.GroupDataJson = gameDataResult.GameData;
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

            group.MapPost("/saveGroupData", async (SaveGroupDataRequest request, Db db, Gaos.Common.UserService userService, 
                GroupData groupDataService, IConfiguration configuration) =>
            {
                const string METHOD_NAME = "saveGroupData()";
                try
                {
                    SaveGroupDataResponse response = new SaveGroupDataResponse();

                    Stopwatch stopwatch = new Stopwatch();

                    if (configuration["gao_profiling"] == "true")
                    {
                        stopwatch.Start();
                    }
                    GetGroupResult userGroup = await userService.GetUserGroup();
                    if (configuration["gao_profiling"] == "true")
                    {
                        stopwatch.Stop();
                        Log.Debug($"{CLASS_NAME}:{METHOD_NAME}: GetUserGroup() took {stopwatch.ElapsedMilliseconds} ms");
                    }
                    if (userGroup == null)
                    {
                        response.IsError = true;
                        response.ErrorMessage = "user not in group";
                        return Results.Json(response);
                    }

                    if (configuration["gao_profiling"] == "true")
                    {
                        stopwatch.Start();
                    }
                    SaveGroupGameDataResult saveResult = await groupDataService.SaveGroupGameData(userGroup, request.GroupDataJson, request.Version, request.IsJsonDiff, request.SlotId);
                    if (configuration["gao_profiling"] == "true")
                    {
                        stopwatch.Stop();
                        Log.Debug($"{CLASS_NAME}:{METHOD_NAME}: SaveGroupGameData() took {stopwatch.ElapsedMilliseconds} ms");
                    }
                    if ((bool)saveResult.IsError)
                    {
                        response.IsError = true;
                        response.ErrorMessage = saveResult.ErrorMessage;
                        return Results.Json(response);
                    }

                    response.IsError = false;
                    response.ErrorMessage = "";
                    return Results.Json(response);

                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    SaveGroupDataResponse response = new SaveGroupDataResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error",
                    };
                    return Results.Json(response);
                }
            });

            group.MapPost("/getOwnersData", async (GetOwnersDataRequest request, Db db, Gaos.Common.UserService userService, 
                GroupData groupDataService, IConfiguration configuration) =>
            {
                const string METHOD_NAME = "getOwnersData()";
                try
                {
                    GetOwnersDataDataResponse response = new GetOwnersDataDataResponse();

                    Stopwatch stopwatch = new Stopwatch();

                    if (configuration["gao_profiling"] == "true")
                    {
                        stopwatch.Start();
                    }
                    GetGroupResult userGroup = await userService.GetUserGroup();
                    if (configuration["gao_profiling"] == "true")
                    {
                        stopwatch.Stop();
                        Log.Debug($"{CLASS_NAME}:{METHOD_NAME}: GetUserGroup() took {stopwatch.ElapsedMilliseconds} ms");
                    }
                    if (userGroup == null)
                    {
                        response.IsError = true;
                        response.ErrorMessage = "user not in group";
                        return Results.Json(response);
                    }

                    String ownersDataJson = await groupDataService.GetOwnersData(userGroup, request.SlotId);

                    response.IsError = false;
                    response.ErrorMessage = "";
                    response.OwnersDataJson = ownersDataJson;
                    return Results.Json(response);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    GetOwnersDataDataResponse response = new GetOwnersDataDataResponse
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
