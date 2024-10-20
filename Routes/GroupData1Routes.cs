#pragma warning disable 8600, 8602, 8604, 8714
using Gaos.Mongo;
using Gaos.Dbo;
using Gaos.Routes.Model.GameDataJson;
using Gaos.Routes.Model.GroupDataJson;
using Serilog;
using static Gaos.Common.UserService;
using System.Diagnostics;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Gaos.Routes.Model.GroupData1Json;
using Microsoft.EntityFrameworkCore;
using Gaos.Dbo.Model;

namespace Gaos.Routes
{
    public static class GroupData1Routes
    {

        public static string CLASS_NAME = typeof(GroupData1Routes).Name;
        public static RouteGroupBuilder GroupData1(this RouteGroupBuilder group)
        {

            group.MapPost("/getCredits", async (GetCreditsRequest request, Db db, Gaos.Common.UserService userService, 
                GroupData groupDataService, IConfiguration configuration) =>
            {
                const string METHOD_NAME = "getCredits()";
                try
                {
                    var response = new GetCreditsResponse();
                    var user = userService.GetUser();


                    if (!await userService.IsUserInGroup(request.GroupId, user.Id))
                    {
                        Log.Warning($"{CLASS_NAME}:{METHOD_NAME}: logged in user is not in the group"); 
                        response.IsError = true;
                        response.ErrorMessage = "logged in user is not in the group";
                        return Results.Json(response);
                    }

                    if (!await userService.IsUserInGroup(request.GroupId, request.UserId))
                    {
                        Log.Warning($"{CLASS_NAME}:{METHOD_NAME}: queried user is not in the group"); 
                        response.IsError = true;
                        response.ErrorMessage = "queried user is not in the group";
                        return Results.Json(response);
                    }


                    var groupCredits = await db.GroupCredits.Where(x => x.UserId == request.UserId && x.GroupId == request.GroupId).FirstOrDefaultAsync();
                    if (groupCredits == null)
                    {
                        Log.Warning($"{CLASS_NAME}:{METHOD_NAME}: queried user has no credits in this group"); 
                        response.IsError = true;
                        response.ErrorMessage = "queried user has no credits in this group";
                        return Results.Json(response);
                    }
                    else
                    {
                        response.IsError = false;
                        response.Credits = groupCredits.Credits;
                        return Results.Json(response);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    GetCreditsResponse  response = new GetCreditsResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error",
                    };
                    return Results.Json(response);
                }
            });

            group.MapPost("/addMyCredits", async (AddCreditsRequest request, 
                Db db, 
                Gaos.Common.UserService userService,
                GroupData groupDataService, 
                IConfiguration configuration,
                Gaos.wsrv.messages.GroupBroadcastService groupBroadcastService) =>
            {
                const string METHOD_NAME = "addMyCredits()";
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {


                        var response = new AddCreditsResponse();

                        var user = userService.GetUser();
                        var group = await userService.GetUserGroup();
                        if (group == null)
                        {
                            Log.Warning($"{CLASS_NAME}:{METHOD_NAME}: no group");
                            response.IsError = true;
                            response.ErrorMessage = "no group";
                            return Results.Json(response);
                        }
                        int groupId;
                        if (group.IsGroupMember)
                        {
                            groupId = group.GroupId;
                        }
                        else if (group.IsGroupOwner)
                        {
                            groupId = group.GroupId;
                        }
                        else
                        {
                            Log.Warning($"{CLASS_NAME}:{METHOD_NAME}: internal error (user is neither group member nor group owner)");
                            response.IsError = true;
                            response.ErrorMessage = "internal error (user is neither group member nor group owner)";
                            return Results.Json(response);
                        }

                        // Get entry from GroupCredits fro the user and group
                        // Read all records from GroupCredits for given user and group
                        var groupCreditsList = await db.GroupCredits.Where(x => x.UserId == user.Id && x.GroupId == groupId).ToListAsync();
                        if (groupCreditsList.Count == 0)
                        {
                            // Create a new entry in GroupCredits for the user and group
                            GroupCredits groupCredits = new GroupCredits
                            {
                                UserId = user.Id,
                                GroupId = groupId,
                                Credits = request.Credits
                            };
                            db.GroupCredits.Add(groupCredits);
                            await db.SaveChangesAsync();

                        }
                        else
                        {
                            // Update the existing entry in GroupCredits for the user and group
                            groupCreditsList[0].Credits += request.Credits;
                            await db.SaveChangesAsync();
                        }
                        transaction.Commit();

                        // Broadcast the new credits to the group
                        await groupBroadcastService.BroadcastCreditsChangeAsync(user.Id, groupId, request.Credits);

                        response.IsError = false;
                        response.Credits = groupCreditsList[0].Credits;
                        return Results.Json(response);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                        AddCreditsResponse response = new AddCreditsResponse
                        {
                            IsError = true,
                            ErrorMessage = "internal error",
                        };
                        transaction.Rollback();
                        return Results.Json(response);
                    }
                }
            });

            group.MapPost("/resetMyCredits", async (ResetCreditsRequest request, 
                Db db, 
                Gaos.Common.UserService userService,
                GroupData groupDataService, 
                IConfiguration configuration,
                Gaos.wsrv.messages.GroupBroadcastService groupBroadcastService) =>
            {
                const string METHOD_NAME = "resetMyCredits()";
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var response = new ResetCreditsResponse();

                        var user = userService.GetUser();
                        var group = await userService.GetUserGroup();
                        if (group == null)
                        {
                            Log.Warning($"{CLASS_NAME}:{METHOD_NAME}: no group");
                            response.IsError = true;
                            response.ErrorMessage = "no group";
                            return Results.Json(response);
                        }
                        int groupId;
                        if (group.IsGroupMember)
                        {
                            groupId = group.GroupId;
                        }
                        else if (group.IsGroupOwner)
                        {
                            groupId = group.GroupId;
                        }
                        else
                        {
                            Log.Warning($"{CLASS_NAME}:{METHOD_NAME}: internal error (user is neither group member nor group owner)");
                            response.IsError = true;
                            response.ErrorMessage = "internal error (user is neither group member nor group owner)";
                            return Results.Json(response);
                        }

                        // Get entry from GroupCredits for the user and group
                        var groupCreditsList = await db.GroupCredits.Where(x => x.UserId == user.Id && x.GroupId == groupId).ToListAsync();
                        if (groupCreditsList.Count == 0)
                        {
                            // Create a new entry in GroupCredits for the user and group
                            GroupCredits groupCredits = new GroupCredits
                            {
                                UserId = user.Id,
                                GroupId = groupId,
                                Credits = request.Credits
                            };
                            db.GroupCredits.Add(groupCredits);
                            await db.SaveChangesAsync();        
                        }
                        else
                        {
                            // Update the existing entry in GroupCredits for the user and group
                            groupCreditsList[0].Credits = request.Credits;
                            await db.SaveChangesAsync();
                        }

                        transaction.Commit();

                        // Broadcast the new credits to the group
                        await groupBroadcastService.BroadcastCreditsChangeAsync(user.Id, groupId, request.Credits);

                        response.IsError = false;
                        response.Credits = groupCreditsList[0].Credits;
                        return Results.Json(response);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                        ResetCreditsResponse response = new ResetCreditsResponse
                        {
                            IsError = true,
                            ErrorMessage = "internal error",
                        };
                        transaction.Rollback();
                        return Results.Json(response);
                    }
                }
            });


            return group;

        }
    }
}
