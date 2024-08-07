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

        public class GroupMemberQqueryResult
        {
            public int UserId { get; set; }
            public string UserName { get; set; }
        };

        public record GetUsersForFriendsSearchResul(int UserId, string UserName, bool IsFriend); 

        public static async  Task<List<GetUsersForFriendsSearchResul>> GetUsersForFriendsSearch(MySqlConnection dbConn, int userId, int maxCount, string userNamePattern)
        {
            const string METHOD_NAME = "GetUsersForFriendsSearch()";
            // Select users and if selected user is already a friend of logged in user (identified by method parameter userId) then  FriendId will be not null and equal to the selected user id. 
            // The friedship to looged in user is determined via membership in group owned by logged in user, any group member is a friend of the group owner.
            var sqlQuery =
@$"
select
    User.Id as UserId,
    User.Name as UserName,
    Friend.Id as FriendId    
from
    User
left join 
(
    select
        GroupMember.UserId as Id
    from
        Groupp
    join GroupMember on Groupp.Id = GroupMember.GroupId
    where
        Groupp.OwnerId = @userId
) as Friend on Friend.Id = User.Id 
where
    User.Name like @userNamePattern
limit  @maxCount
";
            try
            {
                await dbConn.OpenAsync();
                await using var command = dbConn.CreateCommand();
                command.CommandText = sqlQuery;
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@userNamePattern", $"%{userNamePattern}%");
                command.Parameters.AddWithValue("@maxCount", maxCount);
                using var reader = await command.ExecuteReaderAsync();

                List<GetUsersForFriendsSearchResul> result = new List<GetUsersForFriendsSearchResul>();

                while (await reader.ReadAsync())
                {
                    var _UserId = reader.GetInt32(0);
                    var _UserName = reader.GetString(1);
                    int? _FriendId = null;
                    if (!reader.IsDBNull(2))
                        _FriendId = reader.GetInt32(2);
                    bool _IsFriend = _FriendId != null;
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
            // Select users that are friends of logged in user (identified by method parameter userId).
            // The friedship to looged in user is determined via membership in group owned by logged in user, any group member is a friend of the group owner.
            var sqlQuery =
@$"
select
    User.Id as UserId,
    User.Name as UserName
from
    GroupMember
join Groupp on Groupp.Id = GroupMember.GroupId
join User on User.Id = GroupMember.UserId
where
    Groupp.OwnerId = @userId 
limit  @maxCount
";
            try
            {
                await dbConn.OpenAsync();
                await using var command = dbConn.CreateCommand();
                command.CommandText = sqlQuery;
                command.Parameters.AddWithValue("@userId", userId);
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

            group.MapPost("/getMyGroup", async (GetMyGroupReuest getMyGroupReuest,  Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "friends/addFriend";
                try
                {
                    var myGroup = await userService.GetUserGroup();
                    GetMyGroupResponse response = new GetMyGroupResponse
                    {
                        IsError = false,
                        ErrorMessage = null,

                        IsGroupOwner = myGroup.IsGroupOwner,
                        IsGroupMember = myGroup.IsGroupMember,
                        GroupId = myGroup.GroupId,
                        GroupOwnerId = myGroup.GroupOwnerId,
                        GroupOwnerName = myGroup.GroupOwnerName,
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

            group.MapPost("/getUsersList", async (GetUsersListRequest getUsersListRequest, Db db, MySqlConnection dbConn, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "friends/getUsersList";
                try
                {
                    int userId = userService.GetUserId();

                    // Read Groupp of logged in user
                    Groupp group = await db.Groupp
                        .Where(x => x.OwnerId == userId)
                        .FirstAsync();

                    // If logged in user is not Groupp owner, then create a Groupp for him
                    if (group == null)
                    {
                        var _group = new Groupp
                        {
                            Name = userService.GetUser().Name + "'s Groupp",
                            OwnerId = userId,
                        };
                        db.Groupp.Add(_group);
                        await db.SaveChangesAsync();
                        Log.Information($"{CLASS_NAME}:{METHOD_NAME}: created Groupp for user: {userId}");

                        // Read Groupp of logged in user
                        group = await db.Groupp
                            .Where(x => x.OwnerId == userId)
                            .FirstAsync();
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


            group.MapPost("/getGroupMembers", async (GetGroupMembersRequest getGroupMembersRequest, Db db, MySqlConnection dbConn, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "friends/getGroupMembers";
        
                try
                {
                    GetGroupMembersResponse response;

                    int groupId = getGroupMembersRequest.GroupId;
                    int maxCount = getGroupMembersRequest.MaxCount;

                    Groupp group = await db.Groupp
                        .Where(x => x.Id == groupId)
                        .FirstAsync();
                    int groupOwnerId = (group.OwnerId == null)? -1 : (int)group.OwnerId;

                    if (group == null)
                    {
                        Log.Warning($"{CLASS_NAME}:{METHOD_NAME}: no such group: {groupId}");

                        // no such group, return empty list
                        response = new GetGroupMembersResponse
                        {
                            IsError = false,
                            ErrorMessage = null,
                            Users = System.Array.Empty<GroupMembersListUser>()
                        };
                        return Results.Json(response);
                    }

                    // Read User where userId is OwnerId
                    User owner = await db.User
                        .Where(x => x.Id == groupOwnerId)
                        .FirstAsync();

                    // Read group members up to maxCount
                    var query = from groupMember in db.GroupMember
                                join user in db.User on groupMember.UserId equals user.Id
                                where groupMember.GroupId == groupId
                                select new GroupMemberQqueryResult
                                {
                                    UserId = user.Id,
                                    UserName = user.Name,
                                };
                    GroupMemberQqueryResult[] groupMembers = await query
                        .Take(maxCount - 1)
                        .ToArrayAsync();


                    GroupMembersListUser[] members = new GroupMembersListUser[groupMembers.Length - 1];
                    // add owner to the list
                    members[0] = new GroupMembersListUser
                    {
                        Id = owner.Id,
                        Name = owner.Name,
                        IsOwner = true,
                    };
                    // add other members to the list
                    for (int i = 0; i < groupMembers.Length; i++)
                    {
                        if (groupMembers[i].UserId == groupOwnerId)
                            continue;
                        members[i] = new GroupMembersListUser
                        {
                            Id = groupMembers[i].UserId,
                            Name = groupMembers[i].UserName,
                            IsOwner = false,
                        };
                    }

                    // send response
                    response = new GetGroupMembersResponse
                    {
                        IsError = false,
                        ErrorMessage = null,
                        Users = members,
                    };
                    return Results.Json(response);

                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    GetGroupMembersResponse response = new GetGroupMembersResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error",
                    };
                    return Results.Json(response);
                }
            });

            group.MapPost("/addFriend", async (AddFriendRequest addFriendRequest, Db db, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "friends/addFriend";
                try
                {
                    AddFriendResponse response;

                    int userId = userService.GetUserId();
                    int friendId = addFriendRequest.UserId;

                    // Read Groupp of logged in user
                    Groupp group = await db.Groupp
                        .Where(x => x.OwnerId == userId)
                        .FirstAsync();

                    // If logged in user is not Groupp owner, then create a Groupp for him
                    if (group == null)
                    {
                        var _group = new Groupp
                        {
                            Name = userService.GetUser().Name + "'s Groupp",
                            OwnerId = userId,
                        };
                        db.Groupp.Add(_group);
                        await db.SaveChangesAsync();
                        Log.Information($"{CLASS_NAME}:{METHOD_NAME}: created Groupp for user: {userId}");

                        // Read Groupp of logged in user
                        group = await db.Groupp
                            .Where(x => x.OwnerId == userId)
                            .FirstAsync();
                    }


                    // Check if friend is already in Groupp
                    bool friendExists = await db.GroupMember
                        .AnyAsync(x => x.GroupId == group.Id && x.UserId == friendId);
                    if (friendExists)
                    {
                        response = new AddFriendResponse
                        {
                            IsError = false,
                            ErrorMessage = null,
                        };
                        return Results.Json(response);
                    }

                    // Check if friend is member of any other Groupp
                    bool otherGroupExists = await db.GroupMember
                        .Where(x =>  x.UserId == friendId)
                        .AnyAsync();

                    if (otherGroupExists)
                    {
                        response = new AddFriendResponse
                        {
                            IsError = true,
                            ErrorMessage = "friend is already member of another Groupp",
                        };
                        return Results.Json(response);
                    }

                    // Add friend to Groupp
                    var groupMember = new GroupMember
                    {
                        GroupId = group.Id,
                        UserId = friendId,
                    };
                    await db.GroupMember.AddAsync(groupMember);
                    await db.SaveChangesAsync();

                    response = new AddFriendResponse
                    {
                        IsError = false,
                        ErrorMessage = null,
                    };
                    return Results.Json(response);

                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    AddFriendResponse response = new AddFriendResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error",
                    };
                    return Results.Json(response);
                }

            });


            group.MapPost("/removeFriend", async (RemoveFriendRequest removeFriendRequest, Db db, Gaos.Common.UserService userService) => {
                const string METHOD_NAME = "friends/removeFriend";
                try
                {
                    RemoveFriendResponse response;

                    int userId = userService.GetUserId();
                    int friendId = removeFriendRequest.UserId;

                    // Read Groupp of logged in user
                    Groupp group = await db.Groupp
                        .Where(x => x.OwnerId == userId)
                        .FirstAsync();

                    // If logged in user is not Groupp owner, then create a Groupp for him
                    if (group == null)
                    {
                        var _group = new Groupp
                        {
                            Name = userService.GetUser().Name + "'s Groupp",
                            OwnerId = userId,
                        };
                        db.Groupp.Add(_group);
                        await db.SaveChangesAsync();
                        Log.Information($"{CLASS_NAME}:{METHOD_NAME}: created Groupp for user: {userId}");

                        // Read Groupp of logged in user
                        group = await db.Groupp
                            .Where(x => x.OwnerId == userId)
                            .FirstAsync();
                    }

                    // Remove friend from GroupMember
                    var groupMemberList = await db.GroupMember
                        .Where(x => x.GroupId == group.Id && x.UserId == friendId)
                        .ToListAsync();
                    db.GroupMember.RemoveRange(groupMemberList);
                    await db.SaveChangesAsync();

                    response = new RemoveFriendResponse
                    {
                        IsError = false,
                        ErrorMessage = null,
                    };
                    return Results.Json(response);
                        
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    RemoveFriendResponse response = new RemoveFriendResponse
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
