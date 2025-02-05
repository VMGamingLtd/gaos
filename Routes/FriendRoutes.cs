#pragma warning disable 8600, 8602, 8604

using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using Serilog;
using Gaos.Auth;
using Gaos.Dbo;
using Gaos.Routes.Model.GroupJson;
using Gaos.Dbo.Model;
using MySqlConnector;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Security;
using System.Diagnostics;
using Gaos.Routes.Model.FriendJson;

namespace Gaos.Routes
{

    public static class FriendRoutes
    {
        public static int MAX_NUMBER_OF_MESSAGES_IN_ROOM = 100;

        public static string CLASS_NAME = typeof(FriendRoutes).Name;

        public record GetUsersForFriendsSearchResult(int UserId, string UserName, bool IsMyFriend, bool IsMyFriendRequest, bool IsFriendRequestToMe);

        public static async Task<List<GetUsersForFriendsSearchResult>> GetUsersForFriendsSearch(MySqlDataSource dataSource, int userId, int maxCount, string userNamePattern)
        {
            const string METHOD_NAME = "GetUsersForFriendsSearch()";
            // Select users and if selected user is already a friend of logged in user (identified by method parameter userId) then  FriendId will be not null and equal to the selected user id. 
            // The friedship to looged in user is determined via membership in group owned by logged in user, any group member is a friend of the group owner.
            var sqlQuery =
                @$"
                SELECT
                    u.Id AS UserId,
                    u.Name AS UserName,
                    CASE 
                        WHEN 
                            uf.Id IS NOT NULL AND (
                                (uf.UserId = @UserId AND  (uf.FriendId IS NOT NULL AND uf.isFriendAgreement = 1)) OR
                                (uf.FriendId = @UserId AND  (uf.UserId IS NOT NULL AND uf.isFriendAgreement = 1))   
                            )
                        THEN 1
                        ELSE 0
                    END AS IsMyFriend,
                    CASE 
                        WHEN uf.Id IS NOT NULL AND (uf.UserId = @UserId AND (uf.FriendId IS NOT NULL AND  uf.isFriendAgreement = 0))
                        THEN 1
                        ELSE 0
                    END AS IsMyFriendRequest,
                    CASE 
                        WHEN uf.Id IS NOT NULL AND (uf.FriendId = @UserId AND (uf.UserId IS NOT NULL AND  uf.isFriendAgreement = 0)) 
                        THEN 1
                        ELSE 0
                    END AS IsFriendRequestToMe
                FROM
                    User u
                LEFT JOIN
                    UserFriend uf 
                    ON 
                       (
                         (uf.UserId = u.Id AND uf.FriendId = @UserId)
                         OR
                         (uf.UserId = @UserId   AND uf.FriendId = u.Id)
                       )
                WHERE
                    u.Name LIKE @userNamePattern
                    and u.Id != @UserId
                LIMIT 
                    @maxCount;
                ";
            try
            {
                string likePattern;
                if (userNamePattern == null || userNamePattern == "")
                {
                    likePattern = "%";
                }
                else
                {
                    likePattern = $"%{userNamePattern}%";
                }

                using var connection = await dataSource.OpenConnectionAsync();

                using var command = connection.CreateCommand();
                command.CommandText = sqlQuery;
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@userNamePattern", $"%{likePattern}%");
                command.Parameters.AddWithValue("@maxCount", maxCount);
                using var reader = await command.ExecuteReaderAsync();

                List<GetUsersForFriendsSearchResult> result = new List<GetUsersForFriendsSearchResult>();

                while (await reader.ReadAsync())
                {
                    var _UserId = reader.GetInt32(0);
                    var _UserName = reader.GetString(1);
                    int _IsMyFriend = reader.GetInt32(2);
                    int _IsMyFriendRequest = reader.GetInt32(3);
                    int _IsFriendRequestToMe = reader.GetInt32(4);
                    result.Add(new GetUsersForFriendsSearchResult(_UserId, _UserName, _IsMyFriend > 0, _IsMyFriendRequest > 0, _IsFriendRequestToMe > 0));
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

        public record GetMyFriendsSearchResult(int UserId, string UserName);

        public static async Task<List<GetMyFriendsSearchResult>> GetMyFriendsSearch(MySqlDataSource dataSource, int userId, int maxCount, string userNamePattern)
        {
            const string METHOD_NAME = "GetMyFriendsSearch()";
            try
            {
                var sqlQuery =
                    @$"
                        SELECT
                            u.Id AS UserId,
                            u.Name AS UserName  
                        FROM
                            UserFriend uf
                            JOIN User u ON (uf.UserId = u.Id OR uf.FriendId = u.Id)
                        WHERE
                            (uf.UserId = @UserId OR uf.FriendId = @UserId) AND uf.isFriendAgreement = 1
                            AND u.Id != @UserId
                            AND u.Name LIKE @userNamePattern
                        LIMIT   
                            @maxCount;
                    ";

                string likePattern;
                if (userNamePattern == null || userNamePattern == "")
                {
                    likePattern = "%";
                }
                else
                {
                    likePattern = $"%{userNamePattern}%";
                }

                using var connection = await dataSource.OpenConnectionAsync();

                using var command = connection.CreateCommand();
                command.CommandText = sqlQuery;
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@userNamePattern", likePattern);
                command.Parameters.AddWithValue("@maxCount", maxCount);

                using var reader = await command.ExecuteReaderAsync();

                List<GetMyFriendsSearchResult> result = new List<GetMyFriendsSearchResult>();
                while (reader.Read())
                {
                    var _UserId = reader.GetInt32(0);
                    var _UserName = reader.GetString(1);
                    result.Add(new GetMyFriendsSearchResult(_UserId, _UserName));
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

        public record GetFriendRequestsToMeResult(int UserId, string UserName);

        public static async Task<List<GetFriendRequestsToMeResult>> GetFriendRequestsToMe(MySqlDataSource dataSource, int userId, int maxCount)
        {
            const string METHOD_NAME = "GetFriendRequestsToMe()";

            var sqlQuery = @$"
                SELECT
                    u.Id AS UserId,
                    u.Name AS UserName
                FROM
                    UserFriend uf
                    JOIN User u ON uf.UserId = u.Id
                WHERE
                    uf.FriendId = @UserId
                    AND uf.isFriendAgreement = 0
                LIMIT
                    @MaxCount;
            ";

            try
            {
                using var connection = await dataSource.OpenConnectionAsync();
                using var command = connection.CreateCommand();
                command.CommandText = sqlQuery;
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@MaxCount", maxCount);

                using var reader = await command.ExecuteReaderAsync();

                var result = new List<GetFriendRequestsToMeResult>();

                while (await reader.ReadAsync())
                {
                    var requestUserId = reader.GetInt32(0);
                    var requestUserName = reader.GetString(1);

                    result.Add(new GetFriendRequestsToMeResult(requestUserId, requestUserName));
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                throw new Exception("Internal error retrieving friend requests");
            }
        }

        public static async Task<bool> UsersAreFriends(MySqlDataSource dataSource, int userId1, int userId2)
        {
            const string METHOD_NAME = "UsersAreFriends()";

            try
            {
                // Quick check to avoid a DB round-trip if both IDs are the same.
                if (userId1 == userId2)
                {
                    return false;
                }

                using var connection = await dataSource.OpenConnectionAsync();
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT COUNT(*)
                    FROM UserFriend uf
                    WHERE 
                        (
                           (uf.UserId = @userId1 AND uf.FriendId = @userId2)
                           OR
                           (uf.UserId = @userId2 AND uf.FriendId = @userId1)
                        )
                        AND uf.isFriendAgreement = 1
                ";
                command.Parameters.AddWithValue("@userId1", userId1);
                command.Parameters.AddWithValue("@userId2", userId2);

                var result = (long)await command.ExecuteScalarAsync();

                // If count > 0, it means there's at least one matching record
                return result > 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                throw new Exception("internal error");
            }
        }

        public static RouteGroupBuilder Friends(this RouteGroupBuilder group)
        {
            group.MapGet("/hello", (Db db) => "hello");

            group.MapPost("/getUsersForFriendsSearch", async (GetUsersForFriendsSearchRequest request, MySqlDataSource dataSource, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "friends/getUsersForFriendsSearch";
                try
                {
                    var user = userService.GetUser();
                    var usersForFriendsSearch = await GetUsersForFriendsSearch(dataSource, user.Id, request.MaxCount, request.UserNamePattern);

                    GetUsersForFriendsSearchResponse response = new GetUsersForFriendsSearchResponse
                    {
                        IsError = false,
                        Users = usersForFriendsSearch.Select(u => new UserForFriendsSearch
                        {
                            UserId = u.UserId,
                            UserName = u.UserName,
                            IsMyFriend = u.IsMyFriend,
                            IsMyFriendRequest = u.IsMyFriendRequest,
                            IsFriendRequestToMe = u.IsFriendRequestToMe
                        }).ToArray()
                    };

                    return Results.Json(response);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    GetUsersForFriendsSearchResponse response = new GetUsersForFriendsSearchResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error",
                    };
                    return Results.Json(response);
                }
            });

            group.MapPost("/getFriendRequests", async (Model.FriendJson.GetFriendRequestsRequest request, MySqlDataSource dataSource, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "friends/getFriendRequests";

                try
                {
                    var user = userService.GetUser();

                    var pendingRequests = await GetFriendRequestsToMe(dataSource, user.Id, request.MaxCount);

                    FriendRequest[] friendRequests = new FriendRequest[pendingRequests.Count];
                    for (int i = 0; i < pendingRequests.Count; i++)
                    {
                        friendRequests[i] = new FriendRequest
                        {
                            UserId = pendingRequests[i].UserId,
                            UserName = pendingRequests[i].UserName
                        };
                    }

                    var response = new Model.FriendJson.GetFriendRequestsResponse
                    {
                        IsError = false,
                        FriendRequest = friendRequests
                    };

                    // 4) Return as JSON
                    return Results.Json(response);
                }
                catch (Exception ex)
                {
                    // Log and return an error response
                    Log.Error(ex, $"{METHOD_NAME}: error: {ex.Message}");

                    var response = new Model.FriendJson.GetFriendRequestsResponse
                    {
                        IsError = true,
                        ErrorMessage = "Internal error",
                        FriendRequest = Array.Empty<FriendRequest>()
                    };
                    return Results.Json(response);
                }
            });


            group.MapPost("/requestFriend", async (RequestFriendRequest request, Db db, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "friends/requestFriend";
                try
                {
                    int userId = userService.GetUserId();
                    int friendId = request.UserId;

                    if (userId != friendId)
                    {

                        // check if userId, friendId line is in UserFriend table
                        var userFriend = await db.UserFriend.FirstOrDefaultAsync(uf => (uf.UserId == userId && uf.FriendId == friendId) || (uf.UserId == friendId && uf.FriendId == userId));
                        if (userFriend == null)
                        {
                            // insert line
                            var _userFriend = new UserFriend
                            {
                                UserId = userId,
                                FriendId = friendId,
                                IsFriendAgreement = false,
                            };
                            db.UserFriend.Add(_userFriend);
                            db.SaveChanges();
                        }
                        else
                        {
                            if (userFriend.FriendId == friendId)
                            {
                                // Friend request from me to friend already exists amd possibly waiting for friend agreement.
                                ;
                            }
                            else
                            {
                                // Friend request from friend to me already exists amd waiting for my agreement
                                // So I just accept it.
                                userFriend.IsFriendAgreement = true;
                                // save changes
                                db.SaveChanges();
                            }
                        }
                    }

                    RequestFriendResponse response = new RequestFriendResponse
                    {
                        IsError = false,
                        ErrorMessage = "",
                    };
                    return Results.Json(response);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    RequestFriendResponse response = new RequestFriendResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error",
                    };
                    return Results.Json(response);
                }
            });

            group.MapPost("/acceptFriendRequest", async (FriendRequestAcceptRequest request, Db db, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "friends/acceptFriendRequest";
                try
                {
                    int myUserId = userService.GetUserId();
                    int senderUserId = request.UserId;

                    var userFriend = await db.UserFriend.FirstOrDefaultAsync(uf =>
                            uf.UserId == senderUserId
                            && uf.FriendId == myUserId
                            && uf.IsFriendAgreement == false
                    );

                    if (userFriend != null)
                    {
                        userFriend.IsFriendAgreement = true;
                        await db.SaveChangesAsync();
                    }

                    FriendRequestAcceptResponse response = new FriendRequestAcceptResponse
                    {
                        IsError = false,
                        ErrorMessage = null
                    };
                    return Results.Json(response);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{FriendRoutes.CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");

                    // 4) Prepare the error response
                    FriendRequestAcceptResponse response = new FriendRequestAcceptResponse
                    {
                        IsError = true,
                        ErrorMessage = "Internal error accepting friend request"
                    };
                    return Results.Json(response);
                }
            });


            group.MapPost("/removeFriend", async (RemoveFriendRequest request, Db db, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "friends/removeFriend";
                try
                {
                    int myUserId = userService.GetUserId();
                    int myFriendId = request.UserId;

                    var idsToRemove = await db.UserFriend.Where(uf => (uf.UserId == myUserId && uf.FriendId == myFriendId) || (uf.UserId == myFriendId && uf.FriendId == myUserId)).Select(uf => uf.Id).ToListAsync();
                    db.UserFriend.RemoveRange(db.UserFriend.Where(uf => idsToRemove.Contains(uf.Id)));

                    await db.SaveChangesAsync();

                    RemoveFriendResponse response = new RemoveFriendResponse
                    {
                        IsError = false,
                        ErrorMessage = "",
                    };
                    return Results.Json(response);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error:, {ex.Message}");
                    RemoveFriendResponse response = new RemoveFriendResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error",
                    };
                    return Results.Json(response);
                }
            });

            group.MapPost("/getMyFriends", async (GetMyFriendsRequest request, MySqlDataSource dataSource, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "friends/getMyFriends";
                try
                {
                    var user = userService.GetUser();
                    var myFriends = await GetMyFriendsSearch(dataSource, user.Id, request.MaxCount, request.UserNamePattern);
                    GetMyFriendsResponse response = new GetMyFriendsResponse
                    {
                        IsError = false,
                        Users = myFriends.Select(f => new UserForGetMyFriends
                        {
                            UserId = f.UserId,
                            UserName = f.UserName
                        }).ToArray()
                    };
                    return Results.Json(response);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    GetMyFriendsResponse response = new GetMyFriendsResponse
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
