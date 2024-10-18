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

namespace Gaos.Routes
{

    public static class FriendsRoutes
    {
        public static int MAX_NUMBER_OF_MESSAGES_IN_ROOM = 100;

        public static string CLASS_NAME = typeof(FriendsRoutes).Name;

        public class getMyFriends_GroupMemberQqueryResult
        {
            public int UserId { get; set; }
            public string? UserName { get; set; }
        };

        private class getFriendRequests_GroupMemberRequestQueryResult
        {
            public int GroupId { get; set; }
            public string? GroupName { get; set; }
            public int GroupOwnerId { get; set; }
            public string? GroupOwnerName { get; set; }
        };

        public record GetUsersForFriendsSearchResult(int UserId, string UserName, bool IsFriend, bool IsFriendRequest); 

        public static async  Task<List<GetUsersForFriendsSearchResult>> GetUsersForFriendsSearch(MySqlDataSource dataSource, int userId, int maxCount, string userNamePattern)
        {
            const string METHOD_NAME = "GetUsersForFriendsSearch()";
            // Select users and if selected user is already a friend of logged in user (identified by method parameter userId) then  FriendId will be not null and equal to the selected user id. 
            // The friedship to looged in user is determined via membership in group owned by logged in user, any group member is a friend of the group owner.
            var sqlQuery =
@$"
select
    User.Id as UserId,
    User.Name as UserName,
    Friend.Id as FriendId,    
    FriendRequest.Id as FriendRequestId
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
left join 
(
    select
        GroupMemberRequest.UserId as Id
    from
        Groupp
    join GroupMemberRequest on Groupp.Id = GroupMemberRequest.GroupId
    where
        Groupp.OwnerId = @userId
) as FriendRequest on FriendRequest.Id = User.Id 
where
    User.Name like @userNamePattern
limit  @maxCount
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


        public record GetRequestsForFriendRequestSearchResult(int GroupId, int GroupOwnerId, string GroupOwnerName, int TotalCount); 
        public static async  Task<List<GetRequestsForFriendRequestSearchResult>> GetRequestsForFriendRequestSearch(MySqlDataSource dataSource, 
                                                                                                                    int userId, string ownerNamePattern, 
                                                                                                                    int maxCount, bool isCountOnly)
        {
            const string METHOD_NAME = "GetRequestsForFriendRequestSearch()";
            string sqlQuery;
            if (ownerNamePattern != null)
            {
                sqlQuery =
    @$"
select
    Groupp.Id as GroupId,
    Owner.Id as GroupOwnerId,
    Owner.Name as GroupOwnerName
from
    GroupMemberRequest
    join User on GroupMemberRequest.UserId = User.Id
    join Groupp on GroupMemberRequest.GroupId = Groupp.Id
    join User as Owner on Groupp.OwnerId = Owner.Id
where
    User.Id = @userId and
    Owner.Name like @ownerNamePattern
limit  @maxCount
";
            }
            else
            {
                if (!isCountOnly)
                {
                    sqlQuery =
    @$"
select
    Groupp.Id as GroupId,
    Owner.Id as GroupOwnerId,
    Owner.Name as GroupOwnerName
from
    GroupMemberRequest
    join User on GroupMemberRequest.UserId = User.Id
    join Groupp on GroupMemberRequest.GroupId = Groupp.Id
    join User as Owner on Groupp.OwnerId = Owner.Id
where
    User.Id = @userId 
limit  @maxCount
";
                }
                else
                {
                    sqlQuery =
    @$"
select
    count(*) as TotalCount
from
    GroupMemberRequest
    join User on GroupMemberRequest.UserId = User.Id
    join Groupp on GroupMemberRequest.GroupId = Groupp.Id
    join User as Owner on Groupp.OwnerId = Owner.Id
where
    User.Id = @userId 
";
                }
            }

            try
            {
                using var connection = await dataSource.OpenConnectionAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = sqlQuery;
                command.Parameters.AddWithValue("@userId", userId);
                if (ownerNamePattern != null)
                {
                    command.Parameters.AddWithValue("@ownerNamePattern", ownerNamePattern);
                }
                command.Parameters.AddWithValue("@maxCount", maxCount);
                if (ownerNamePattern != null)
                {
                    command.Parameters.AddWithValue("@ownerNamePattern", ownerNamePattern);
                }
                using var reader = await command.ExecuteReaderAsync();
                if (!isCountOnly)
                {

                    List<GetRequestsForFriendRequestSearchResult> result = new List<GetRequestsForFriendRequestSearchResult>();
                    while (await reader.ReadAsync())
                    {
                        var _GroupId = reader.GetInt32(0);
                        var _GroupOwnerId = reader.GetInt32(1);
                        var _GroupOwnerName = reader.GetString(2);
                        result.Add(new GetRequestsForFriendRequestSearchResult(_GroupId, _GroupOwnerId, _GroupOwnerName, 0));
                    }

                    return result;
                }
                else
                {
                    if (await reader.ReadAsync())
                    {
                        var _TotalCount = reader.GetInt32(0);
                        List<GetRequestsForFriendRequestSearchResult> result = new List<GetRequestsForFriendRequestSearchResult>();
                        result.Add(new GetRequestsForFriendRequestSearchResult(0, 0, "", _TotalCount));
                        return result;
                    }
                    else
                    {
                        Log.Error($"{CLASS_NAME}:{METHOD_NAME}: no count returned from db");
                        throw new Exception("no count returned from db");
                    }
                }
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

            group.MapPost("/getMyGroup", async (GetMyGroupRequest getMyGroupReuest,  Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "friends/getMyGroup";
                try
                {
                    var myGroup = await userService.GetUserGroup();
                    if (myGroup != null)
                    {
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
                    else
                    {
                        GetMyGroupResponse response = new GetMyGroupResponse
                        {
                            IsError = false,
                            ErrorMessage = null,

                            IsGroupOwner = false,
                            IsGroupMember = false,
                        };

                        return Results.Json(response);
                    }
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

            group.MapPost("/getUsersList", async (GetUsersListRequest getUsersListRequest, Db db, MySqlDataSource dataSource, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "friends/getUsersList";
                try
                {
                    int userId = userService.GetUserId();

                    // Read Groupp of logged in user
                    Groupp group = await db.Groupp
                        .Where(x => x.OwnerId == userId).FirstOrDefaultAsync();

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
                    var users = await GetUsersForFriendsSearch(dataSource, userId, getUsersListRequest.MaxCount, getUsersListRequest.FilterUserName);
                    responseUsers = new UsersListUser[users.Count];
                    for (int i = 0; i < users.Count; i++)
                    {
                        responseUsers[i] = new UsersListUser
                        {
                            Id = users[i].UserId,
                            Name = users[i].UserName,
                            IsFriend = users[i].IsFriend,
                            IsFriendRequest = users[i].IsFriendRequest,
                        };
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



            group.MapPost("/getMyFriends", async (GetMyFriendsRequest getMyFriendsRequest, Db db, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "friends/getMyFriends";
        
                try
                {
                    GetMyFriendsResponse response;

                    int maxCount = getMyFriendsRequest.MaxCount;
                    int userId = userService.GetUserId();

                    bool userIsGroupOwner = false;
                    bool userIsGroupMember = false;

                    // Read Groupp of logged in user
                    Groupp group = await db.Groupp
                        .Where(x => x.OwnerId == userId)
                        .FirstOrDefaultAsync();
                    if ((group != null) && (group.Id == getMyFriendsRequest.GroupId))
                    {
                        userIsGroupOwner = true;
                    }
                    else
                    {
                        userIsGroupMember = await db.GroupMember
                            .AnyAsync(x => x.GroupId == getMyFriendsRequest.GroupId && x.UserId == userId);
                    }

                    int groupId = getMyFriendsRequest.GroupId;
                    GroupMembersListUser[] members;
                    int membersTotalCount = 0;

                    if (userIsGroupOwner || userIsGroupMember)
                    {
                        if (!getMyFriendsRequest.IsCountOnly)
                        {
                            // Read group members up to maxCount
                            var query = from groupMember in db.GroupMember
                                        join user in db.User on groupMember.UserId equals user.Id
                                        where groupMember.GroupId == groupId
                                        select new getMyFriends_GroupMemberQqueryResult
                                        {
                                            UserId = user.Id,
                                            UserName = user.Name,
                                        };
                            getMyFriends_GroupMemberQqueryResult[] groupMembers = await query
                                .Take(maxCount)
                                .ToArrayAsync();


                            members = new GroupMembersListUser[groupMembers.Length];
                            // add other members to the list
                            for (int i = 0; i < groupMembers.Length; i++)
                            {
                                members[i] = new GroupMembersListUser
                                {
                                    UserId = groupMembers[i].UserId,
                                    UserName = groupMembers[i].UserName,
                                };
                            }

                            // send response
                            response = new GetMyFriendsResponse
                            {
                                IsError = false,
                                ErrorMessage = null,
                                Users = members,
                            };
                            return Results.Json(response);
                        }
                        else
                        {
                            // Read total count of group members
                            membersTotalCount = await db.GroupMember
                                .Where(x => x.GroupId == groupId)
                                .CountAsync();
                            // send response
                            response = new GetMyFriendsResponse
                            {
                                IsError = false,
                                ErrorMessage = null,
                                TotalCount = membersTotalCount,
                            };
                            return Results.Json(response);
                        }
                    }
                    else
                    {
                        if (!getMyFriendsRequest.IsCountOnly)
                        {
                            members = Array.Empty<GroupMembersListUser>();
                            // send response
                            response = new GetMyFriendsResponse
                            {
                                IsError = false,
                                ErrorMessage = null,
                                Users = members,
                            };
                            return Results.Json(response);
                        }
                        else
                        {
                            // send response
                            response = new GetMyFriendsResponse
                            {
                                IsError = false,
                                ErrorMessage = null,
                                TotalCount = 0,
                            };
                            return Results.Json(response);
                        }
                    }
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

            // Files friend request, which will have to be accepted or rejecetd by potential friend user

            group.MapPost("/addFriend", async (AddFriendRequest addFriendRequest, Db db, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "friends/addFriend";
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        AddFriendResponse response;

                        int userId = userService.GetUserId();
                        int friendId = addFriendRequest.UserId;

                        // Read Groupp of logged in user
                        Groupp group = await db.Groupp
                            .Where(x => x.OwnerId == userId)
                            .FirstOrDefaultAsync();

                        if (group == null)
                        {
                            // If logged in user is not Groupp owner, then create an empty Groupp for him
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

                        // If logged in user is already a member of some group, he cannot start his own group.
                        // Check if logged in user is already a member of some group.
                        bool userIsGroupMember = await db.GroupMember
                            .AnyAsync(x => x.UserId == userId);
                        if (userIsGroupMember)
                        {
                            Log.Warning($"{CLASS_NAME}:{METHOD_NAME}: logged in user is already a member of some group, userId: {userId}");
                            response = new AddFriendResponse
                            {
                                IsError = true,
                                ErrorMessage = "logged in user is already a member of some group, he cannot has his own group",
                            };
                            return Results.Json(response);
                        }

                        // Check if friend to be added and group owner are same person
                        if (group.OwnerId == friendId)
                        {
                            Log.Warning($"{CLASS_NAME}:{METHOD_NAME}: friend is group owner, groupId: {group.Id}, userId: {friendId}");
                            response = new AddFriendResponse
                            {
                                IsError = true,
                                ErrorMessage = "friend is group owner",
                            };
                            return Results.Json(response);
                        }


                        // Check if friend is already in Groupp
                        bool friendExists = await db.GroupMember
                            .AnyAsync(x => x.GroupId == group.Id && x.UserId == friendId);
                        if (friendExists)
                        {
                            Log.Warning($"{CLASS_NAME}:{METHOD_NAME}: friend is already group member, groupId: {group.Id}, userId: {friendId}");
                            response = new AddFriendResponse
                            {
                                IsError = true,
                                ErrorMessage = $"friend is already group member, groupId: {group.Id}",
                            };
                            return Results.Json(response);
                        }

                        // Check if friend is member of any other Groupp
                        bool otherGroupExists = await db.GroupMember
                            .Where(x => x.UserId == friendId)
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

                        // Check group.Id, friendId is Already in GroupMemberRequest
                        bool groupMemberRequestExists = await db.GroupMemberRequest
                            .AnyAsync(x => x.GroupId == group.Id && x.UserId == friendId);
                        if (groupMemberRequestExists)
                        {
                            Log.Warning($"{CLASS_NAME}:{METHOD_NAME}: group member request already exists, groupId: {group.Id}, userId: {friendId}");
                            response = new AddFriendResponse
                            {
                                IsError = false,
                                ErrorMessage = null,
                            };
                            return Results.Json(response);
                        }

                        // Add friend to GrouppMemberRequest
                        var groupMemberRequest = new GroupMemberRequest
                        {
                            GroupId = group.Id,
                            UserId = friendId,
                            RequestDate = DateTime.Now,
                        };
                        await db.GroupMemberRequest.AddAsync(groupMemberRequest);
                        await db.SaveChangesAsync();
                        transaction.Commit();

                        response = new AddFriendResponse
                        {
                            IsError = false,
                            ErrorMessage = null,
                        };
                        return Results.Json(response);

                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                        AddFriendResponse response = new AddFriendResponse
                        {
                            IsError = true,
                            ErrorMessage = "internal error",
                        };
                        return Results.Json(response);
                    }
                }

            });

            group.MapPost("/revokeFriendRequest", async (RevokeFriendRequestRequest revokeFriendRequestRequest, Db db, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "friends/revokeFriendRequest";
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        int userId = userService.GetUserId();
                        int friendId = revokeFriendRequestRequest.UserId;

                        // Read Groupp of logged in user
                        Groupp group = await db.Groupp
                            .Where(x => x.OwnerId == userId)
                            .FirstOrDefaultAsync();

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

                        // Remove all lines from GroupMemberRequest table where userId and friendId and groupId is group.Id
                        db.GroupMemberRequest.RemoveRange(db.GroupMemberRequest
                            .Where(x => x.GroupId == group.Id && x.UserId == friendId));
                        db.SaveChanges();

                        transaction.Commit();

                        RevokeFriendRequestResponse response = new RevokeFriendRequestResponse
                        {
                            IsError = false,
                            ErrorMessage = null,
                        };
                        return Results.Json(response);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                        RevokeFriendRequestResponse response = new RevokeFriendRequestResponse
                        {
                            IsError = true,
                            ErrorMessage = "internal error",
                        };
                        return Results.Json(response);
                    }
                }
            });


            group.MapPost("/getFriendRequests", async (GetFriendRequestsRequest getFriendRequestsRequest, Db db, MySqlDataSource dataSource, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "friends/getFriwendRequests";
                using (var transaction = db.Database.BeginTransaction())
                {
                    GetFriendRequestsResponse response;
                    try
                    {
                        int userId = userService.GetUserId();
                        string ownerNamePattern = getFriendRequestsRequest.OwnerNamePattern;
                        int maxCount = getFriendRequestsRequest.MaxCount;


                        if (!getFriendRequestsRequest.IsCountOnly)
                        {
                            var friendRequestsSerach = await GetRequestsForFriendRequestSearch(dataSource, userId, ownerNamePattern, maxCount, false);


                            List<GetFriendRequestsResponseListItem> friendRequests = new List<GetFriendRequestsResponseListItem>();
                            foreach (var item in friendRequestsSerach)
                            {
                                friendRequests.Add(new GetFriendRequestsResponseListItem
                                {
                                    GroupId = item.GroupId,
                                    GroupOwnerId = item.GroupOwnerId,
                                    GroupOwnerName = item.GroupOwnerName,
                                });
                            }

                            response = new GetFriendRequestsResponse
                            {
                                IsError = false,
                                ErrorMessage = null,
                                FriendRequests = friendRequests,
                            };
                            transaction.Commit();
                            return Results.Json(response);
                        }
                        else
                        {
                            var friendRequestsSerach = await GetRequestsForFriendRequestSearch(dataSource, userId, ownerNamePattern, maxCount, true);
                            int totalCount = friendRequestsSerach[0].TotalCount;
                            response = new GetFriendRequestsResponse
                            {
                                IsError = false,
                                ErrorMessage = null,
                                TotalCount = totalCount,
                            };
                            transaction.Commit();
                            return Results.Json(response);
                        }

                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                        response = new GetFriendRequestsResponse
                        {
                            IsError = true,
                            ErrorMessage = "internal error",
                        };
                        return Results.Json(response);
                    }
                }
            });

            group.MapPost("/acceptFriendRequest", async (AcceptFriendRequestRequest acceptFriendRequest, Db db, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "friends/acceptFriendRequest";
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        AcceptFriendRequestResponse response;

                        int groupId = acceptFriendRequest.GroupId;
                        int userId = userService.GetUserId();

                        // Read GroupMemberRequest for userId and groupId
                        GroupMemberRequest groupMemberRequest = await db.GroupMemberRequest
                            .Where(x => x.GroupId == groupId && x.UserId == userId)
                            .FirstAsync();
                        if (groupMemberRequest == null)
                        {
                            transaction.Rollback();
                            Log.Warning($"{CLASS_NAME}:{METHOD_NAME}: no such group member request: {groupId}, {userId}");
                            response = new AcceptFriendRequestResponse
                            {
                                IsError = false,
                                ErrorMessage = null,
                            };
                            return Results.Json(response);
                        }

                        // Read GroupMember for userId and groupId
                        GroupMember groupMember = await db.GroupMember
                            .Where(x => x.GroupId == groupId && x.UserId == userId)
                            .FirstOrDefaultAsync();
                        if (groupMember != null)
                        {
                            transaction.Rollback();
                            Log.Warning($"{CLASS_NAME}:{METHOD_NAME}: user is already meember of this group, groupId: {groupId}, userId: {userId}");
                            response = new AcceptFriendRequestResponse
                            {
                                IsError = false,
                                ErrorMessage = null,
                            };
                            return Results.Json(response);
                        }

                        // Read GroupMember for userId and any other groupId
                        GroupMember groupMemberOther = await db.GroupMember
                            .Where(x => x.UserId == userId && x.GroupId != groupId)
                            .FirstOrDefaultAsync();
                        if (groupMemberOther != null)
                        {
                            transaction.Rollback();
                            Log.Warning($"{CLASS_NAME}:{METHOD_NAME}: user is already member of other group, groupId: {groupMemberOther.GroupId}, userId {userId}");
                            response = new AcceptFriendRequestResponse
                            {
                                IsError = true,
                                ErrorMessage = "lready member of other group",
                            };
                            return Results.Json(response);
                        }

                        // Create GroupMember
                        var groupMemberNew = new GroupMember
                        {
                            GroupId = groupId,
                            UserId = userId,
                        };
                        db.GroupMember.Add(groupMemberNew);

                        // Remove GroupMemberRequest
                        db.GroupMemberRequest.Remove(groupMemberRequest);

                        await db.SaveChangesAsync();
                        transaction.Commit();

                        response = new AcceptFriendRequestResponse
                        {
                            IsError = false,
                            ErrorMessage = null,
                        };
                        return Results.Json(response);

                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                        AcceptFriendRequestResponse response = new AcceptFriendRequestResponse
                        {
                            IsError = true,
                            ErrorMessage = "internal error",
                        };
                        return Results.Json(response);
                    }
                }
            });

            group.MapPost("/rejectFriendRequest", async (RejectFriendRequestRequest rejectFriendRequest, Db db, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "friends/rejectFriendRequest";
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        RejectFriendRequestResponse response;

                        int groupId = rejectFriendRequest.GroupId;
                        int userId = userService.GetUserId();

                        // Read GroupMemberRequest for userId and groupId
                        GroupMemberRequest groupMemberRequest = await db.GroupMemberRequest
                            .Where(x => x.GroupId == groupId && x.UserId == userId)
                            .FirstAsync();

                        if (groupMemberRequest == null)
                        {
                            transaction.Rollback();
                            Log.Warning($"{CLASS_NAME}:{METHOD_NAME}: no such group member request: {groupId}, {userId}");
                            response = new RejectFriendRequestResponse
                            {
                                IsError = false,
                                ErrorMessage = null,
                            };
                            return Results.Json(response);
                        }

                        // Remove GroupMemberRequest
                        db.GroupMemberRequest.Remove(groupMemberRequest);
                        await db.SaveChangesAsync();
                        transaction.Commit();

                        response = new RejectFriendRequestResponse
                        {
                            IsError = false,
                            ErrorMessage = null,
                        };
                        return Results.Json(response);

                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                        RejectFriendRequestResponse response = new RejectFriendRequestResponse
                        {
                            IsError = true,
                            ErrorMessage = "internal error",
                        };
                        return Results.Json(response);
                    }
                }
            });



            group.MapPost("/removeFromGroup", async (RemoveFromGroupRequest removeFromGroupRequest, Db db, Gaos.Common.UserService userService) => {
                const string METHOD_NAME = "friends/removeFromGroup";
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        RemoveFromGroupResponse response;

                        int userId = userService.GetUserId();
                        int friendId = removeFromGroupRequest.UserId;

                        // Read Groupp of logged in user
                        Groupp group = await db.Groupp
                            .Where(x => x.OwnerId == userId)
                            .FirstOrDefaultAsync();

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
                        transaction.Commit();

                        response = new RemoveFromGroupResponse
                        {
                            IsError = false,
                            ErrorMessage = null,
                        };
                        return Results.Json(response);

                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                        RemoveFromGroupResponse response = new RemoveFromGroupResponse
                        {
                            IsError = true,
                            ErrorMessage = "internal error",
                        };
                        return Results.Json(response);
                    }
                }

            });

            group.MapPost("/leaveGroup", async (LeaveGroupRequest leaveGroupRequest, Db db, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "friends/leaveGroup";
                using (var transaction = db.Database.BeginTransaction())
                {
                    LeaveGroupResponse response;
                    try
                    {
                        int userId = userService.GetUserId();

                        // Reomove all userId records from GroupMember 
                        await db.Database.ExecuteSqlRawAsync("delete from GroupMember where UserId = {0}", userId);
                        await db.SaveChangesAsync();
                        transaction.Commit();

                        response = new LeaveGroupResponse
                        {
                            IsError = false,
                            ErrorMessage = null,
                        };
                        return Results.Json(response);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                        response = new LeaveGroupResponse
                        {
                            IsError = true,
                            ErrorMessage = "internal error",
                        };
                        return Results.Json(response);
                    }
                }
            });

            return group;

        }
    }
}
