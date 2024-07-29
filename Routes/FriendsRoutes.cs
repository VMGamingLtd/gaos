#pragma warning disable 8600, 8602, 8604

using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using Serilog;
using Gaos.Auth;
using Gaos.Dbo;
using Gaos.Routes.Model.FriendsJson;
using Gaos.Dbo.Model;
using MySqlConnector;

namespace Gaos.Routes
{

    public static class FriendsRoutes
    {
        public static int MAX_NUMBER_OF_MESSAGES_IN_ROOM = 100;

        public static string CLASS_NAME = typeof(FriendsRoutes).Name;


        public record GetUsersForFriendsSearchResul(int UserId, string UserName, bool IsFriend); 
        public static async  Task<List<GetUsersForFriendsSearchResul>> GetUsersForFriendsSearch(MySqlConnection dbConn, int userId, int maxCount, string userNamePattern)
        {
            const string METHOD_NAME = "GetUsersForFriendsSearch()";
            var sqlQuery =
@$"
select
  User.Id as UserId,
  User.Name as UserName,
  Friend.ChatRoomOwnerId as ChatRoomOwnerId
from
  User
left  join
  (select
      ChatRoomMember.UserId as UserId,
      ChatRoom.OwnerId as ChatRoomOwnerId
  from
    ChatRoom
  join ChatRoomMember on ChatRoom.Id = ChatRoomMember.ChatRoomId
  where
    ChatRoom.OwnerId = @ownerId
  ) as Friend on Friend.UserId = User.Id 
where
  User.Name like @userNamePattern
limit  @maxCount
";
            try
            {
                await dbConn.OpenAsync();
                await using var command = dbConn.CreateCommand();
                command.CommandText = sqlQuery;
                command.Parameters.AddWithValue("@ownerId", userId);
                command.Parameters.AddWithValue("@userNamePattern", $"%{userNamePattern}%");
                command.Parameters.AddWithValue("@maxCount", maxCount);
                using var reader = await command.ExecuteReaderAsync();

                List<GetUsersForFriendsSearchResul> result = new List<GetUsersForFriendsSearchResul>();

                while (await reader.ReadAsync())
                {
                    var _UserId = reader.GetInt32(0);
                    var _UserName = reader.GetString(1);
                    int? _ChatRoomOwnerId = null;
                    if (!reader.IsDBNull(2))
                        _ChatRoomOwnerId = reader.GetInt32(2);
                    bool _IsFriend = _ChatRoomOwnerId != null;
                    result.Add(new GetUsersForFriendsSearchResul(_UserId, _UserName, _IsFriend));
                }
                reader.Close();

                return result;

            } 
            catch (Exception ex)
            {
                Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                throw new Exception("internal error");
            }


        }

        public record GetUserFriendsResult(int UserId, string UserName);
        public static async Task<List<GetUserFriendsResult>> GetUserFriends(MySqlConnection dbConn, int userId, int maxCount)
        {
            const string METHOD_NAME = "GetUserFriends()";
            var sqlQuery =
@$"
select
      ChatRoomMember.UserId as UserId,
      User.Name as UserName
from
    ChatRoom
join ChatRoomMember on ChatRoom.Id = ChatRoomMember.ChatRoomId
join User on ChatRoomMember.UserId = User.Id
where
    ChatRoom.OwnerId = @ownerId and
    ChatRoomMember.UserId != @ownerId
limit  @maxCount
";
            try
            {
                await dbConn.OpenAsync();
                await using var command = dbConn.CreateCommand();
                command.CommandText = sqlQuery;
                command.Parameters.AddWithValue("@ownerId", userId);
                command.Parameters.AddWithValue("@maxCount", maxCount);
                using var reader = await command.ExecuteReaderAsync();

                List<GetUserFriendsResult> result = new List<GetUserFriendsResult>();
                while (await reader.ReadAsync())
                {
                    var _UserId = reader.GetInt32(0);
                    var _UserName = reader.GetString(1);
                    result.Add(new GetUserFriendsResult(_UserId, _UserName));
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                throw new Exception("internal error");
            }
        }



        public static RouteGroupBuilder GroupFriends(this RouteGroupBuilder group)
        {
            group.MapGet("/hello", (Db db) => "hello");

            _ = group.MapPost("/getUsersList", async (GetUsersListRequest getUsersListRequest, Db db, MySqlConnection dbConn, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "friends/getUsersList";
                try
                {
                    // If this user is not chatroom owner, then create a chatroom for him

                    int userId = userService.GetUserId();
                    bool chatRoomExists = await db.ChatRoom
                        .AnyAsync(x => x.OwnerId == userId);
                    // create chatroom if not exists
                    if (!chatRoomExists)
                    {
                        var chatRoom = new ChatRoom
                        {
                            Name = userService.GetUser().Name + "'s chatroom",
                            OwnerId = userId,
                        };
                        db.ChatRoom.Add(chatRoom);
                        await db.SaveChangesAsync();
                        Log.Information($"{CLASS_NAME}:{METHOD_NAME}: created chatroom for user: {userId}");
                    }

                    UsersListUser[] responseUsers;
                    if (getUsersListRequest.FilterUserName != null && getUsersListRequest.FilterUserName.Length > 0)
                    {
                        var users = await GetUsersForFriendsSearch(dbConn, userId, getUsersListRequest.MaxCount, getUsersListRequest.FilterUserName);
                        responseUsers = new UsersListUser[users.Count];
                        for (int i = 0; i < users.Count; i++)
                        {
                            responseUsers[i] = new UsersListUser
                            {
                                Id = users[i].UserId,
                                Name = users[i].UserName,
                                IsFriend = users[i].IsFriend,
                            };
                        }
                    }
                    else
                    {
                        var users = await GetUserFriends(dbConn, userId, getUsersListRequest.MaxCount);
                        responseUsers = new UsersListUser[users.Count];
                        for (int i = 0; i < users.Count; i++)
                        {
                            responseUsers[i] = new UsersListUser
                            {
                                Id = users[i].UserId,
                                Name = users[i].UserName,
                                IsFriend = true,
                            };
                        }
                    }

                    // Create response
                    GetUsersListResponse response = new GetUsersListResponse
                    {
                        IsError = false,
                        ErrorMessage = null,
                        Users = responseUsers,
                    };


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
