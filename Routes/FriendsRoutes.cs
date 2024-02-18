#pragma warning disable 8600, 8602, 8604

using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using Serilog;
using Gaos.Auth;
using Gaos.Dbo;
using Gaos.Routes.Model.FriendsJson;
using Gaos.Dbo.Model;

namespace Gaos.Routes
{

    public static class FriendsRoutes
    {
        public static int MAX_NUMBER_OF_MESSAGES_IN_ROOM = 100;

        public static string CLASS_NAME = typeof(FriendsRoutes).Name;
        public static RouteGroupBuilder GroupFriends(this RouteGroupBuilder group)
        {
            group.MapGet("/hello", (Db db) => "hello");

            group.MapPost("/getUsersList", async (GetUsersListRequest getUsersListRequest, Db db, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "friends/getUsersList";
                try
                {
                    // Read all users from database
                    User[] users = await db.User.ToArrayAsync();

                    // Create response
                    GetUsersListResponse response = new GetUsersListResponse
                    {
                        IsError = false,
                        ErrorMessage = null,
                        Users = new UsersListUser[users.Length],
                    };

                    // Fill response
                    for (int i = 0; i < users.Length; i++)
                    {
                        response.Users[i] = new UsersListUser
                        {
                            Id = users[i].Id,
                            Name = users[i].Name,
                        };
                    }

                    // Return response
                    return Results.Json(response);

                }
                catch (Exception ex) 
                { 
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    GetUsersListResponse response = new GetUsersListResponse
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
