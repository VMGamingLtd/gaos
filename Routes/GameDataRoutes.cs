#pragma warning disable 8600, 8602, 8604, 8714
using Gaos.Dbo.Model;
using Gaos.Routes.Model.GameDataJson;
using Gaos.Dbo;
using Serilog;

namespace Gaos.Routes
{
    public static class GameDataRoutes
    {

        public static string CLASS_NAME = typeof(GameDataRoutes).Name;
        public static RouteGroupBuilder GroupGameData(this RouteGroupBuilder group)
        {

            group.MapPost("/ensureNewSlot", async (EnsureNewSlotRequest request, Db db, Gaos.Common.UserService userService, Gaos.Mongo.GameData gameDataService) =>
            {
                const string METHOD_NAME = "ensureNewSlot()";
                try 
                {
                    EnsureNewSlotResponse response;
                    int userId = request.UserId;
                    int slotId = request.SlotId;

                    if (userId != userService.GetUserId())
                    {
                        response = new EnsureNewSlotResponse
                        {
                            IsError = true,
                            ErrorMessage = "request.UserId does not match user id of authorized user"
                        };
                        Log.Warning($"{CLASS_NAME}:{METHOD_NAME}: request.UserId does not match user id of authorized user");
                        return Results.Json(response);
                    
                    }

                    // Ensure new slot
                    await gameDataService.EnsureNewGameSlot(userId, slotId, userService.GetUser().Name);

                    response = new EnsureNewSlotResponse
                    {
                        IsError = false,
                        ErrorMessage = "",
                    };

                    return Results.Json(response);


                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    EnsureNewSlotResponse response = new EnsureNewSlotResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error",
                    };
                    return Results.Json(response);
                }
            });

            group.MapPost("/userGameDataGet", async (UserGameDataGetRequest request, Db db, Gaos.Common.UserService userService, Gaos.Mongo.GameData gameDataService) => 
            {
                const string METHOD_NAME = "userGameDataGet()";
                try 
                {
                    UserGameDataGetResponse response;
                    int userId = request.UserId;
                    int slotId = request.SlotId;

                    if (userId != userService.GetUserId())
                    {
                        response = new UserGameDataGetResponse
                        {
                            IsError = true,
                            ErrorMessage = "request.UserId does not match user id of authorized user"
                        };
                        Log.Warning($"{CLASS_NAME}:{METHOD_NAME}: request.UserId does not match user id of authorized user");
                        return Results.Json(response);
                    
                    }

                    // GaDataJson

                    string gameDataJson = await gameDataService.GetGameDataAsync(userId, slotId);

                    response = new UserGameDataGetResponse
                    {
                        IsError = false,
                        ErrorMessage = "",

                        GameDataJson = gameDataJson,
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

            group.MapPost("/userGameDataSave", async (UserGameDataSaveRequest request, Db db, Gaos.Common.UserService userService, Gaos.Mongo.GameData gameDataService) => 
            {
                const string METHOD_NAME = "userGameDataSave()";
                try 
                {
                    UserGameDataSaveResponse response;

                    if (request.UserId != userService.GetUserId())
                    {
                        response = new UserGameDataSaveResponse
                        {
                            IsError = true,
                            ErrorMessage = "request.UserId does not match user id of authorized user"
                        };
                        Log.Warning($"{CLASS_NAME}:{METHOD_NAME}: request.UserId does not match user id of authorized user");
                        return Results.Json(response);
                    }


                    try
                    {
                        await gameDataService.SaveGameDataAsync(userService.GetUserId(), request.SlotId, request.GameDataJson);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error, saving GameDataJson: {ex.Message}");
                        throw new Exception("could no save GameDataJson");
                    }   


                    response = new UserGameDataSaveResponse
                    {
                        IsError = false,
                        ErrorMessage = "",
                    };
                    return Results.Json(response);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    UserGameDataSaveResponse response = new UserGameDataSaveResponse
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
