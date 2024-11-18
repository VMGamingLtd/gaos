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
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Security;
using System.Diagnostics;

namespace Gaos.Routes
{

    public static class FriendRoutes
    {
        public static int MAX_NUMBER_OF_MESSAGES_IN_ROOM = 100;

        public static string CLASS_NAME = typeof(FriendRoutes).Name;

        public record GetUsersForFriendsSearchResult(int UserId, string UserName, bool IsFriend, bool IsFriendRequest); 

        public static async  Task<List<GetUsersForFriendsSearchResult>> GetUsersForFriendsSearch(MySqlDataSource dataSource, int userId, int maxCount, string userNamePattern)
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
                (uf.UserId = @UserId AND  (uf.FriendIs IS NOT NULL AND uf.isFriendAgreement = 1) OR
                (uf.FriendId = @UserId AND  (uf.UserId IS NOT NULL AND uf.isFriendAgreement = 1))   
        THEN 1
        ELSE 0
    END AS IsMyFriend,
    CASE 
        WHEN uf.Id IS NOT NULL AND (uf.UserId = @UserId AND (uf.FriendId IS NOT NULL AND  uf.isFriendAgreement = 0) 
        THEN 1
        ELSE 0
    END AS IsMyFriendRequest,
    CASE 
        WHEN uf.Id IS NOT NULL AND (uf.FriendId = @UserId AND (uf.UserId IS NOT NULL AND  uf.isFriendAgreement = 0) 
        THEN 1
        ELSE 0
    END AS IsFriendRequestToMe
FROM
    User u
LEFT JOIN
    UserFriend uf 
    ON (u.Id = uf.UserId) OR (u.Id = uf.FriendId)
WHERE
    u.Name LIKE @userNamePattern
LIMIT 
    @maxCount;
";
            try
            {
                string likePattern;
                if (userNamePattern == null)
                {
                    likePattern = "%";
                }
                else
                {
                    likePattern = $"%{userNamePattern}%";
                }
                using var connection = await dataSource.OpenConnectionAsync();
                await using var command = connection.CreateCommand();
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
                    int? _FriendId = null;
                    if (!reader.IsDBNull(2))
                        _FriendId = reader.GetInt32(2);
                    bool _IsFriend = _FriendId != null;
                    int? _FriendRequestId = null;
                    if (!reader.IsDBNull(3))
                        _FriendRequestId = reader.GetInt32(3);
                    bool _IsFriendRequest = _FriendRequestId != null;
                    result.Add(new GetUsersForFriendsSearchResult(_UserId, _UserName, _IsFriend, _IsFriendRequest));
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

        public static RouteGroupBuilder Friends(this RouteGroupBuilder group)
        {
            group.MapGet("/hello", (Db db) => "hello");

            group.MapPost("/getMyGroup", async (GetMyGroupRequest getMyGroupReuest,  Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "friends/getMyGroup";
                try
                {
                    GetMyGroupResponse response = new GetMyGroupResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error",
                    };
                    return Results.Json(response);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    GetMyGroupResponse response = new GetMyGroupResponse
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
