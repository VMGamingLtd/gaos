#pragma warning disable 8625, 8603, 8629, 8604, 8618

using Gaos.Dbo.Model;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Serilog;
namespace Gaos.Common
{

    public class UserService
    {
        private static string CLASS_NAME = typeof(UserService).Name;
        private Gaos.Auth.TokenService TokenService = null;
        private HttpContext Context = null;
        private Gaos.Dbo.Db Db = null;
        private MySqlDataSource DataSource = null;

        private Gaos.Dbo.Model.User? User = null;
        private GetGroupResult getGroupResult = null;
        private bool isGetGroupResult = false;

        public UserService(HttpContext context, Auth.TokenService tokenService, Gaos.Dbo.Db db, MySqlDataSource dataSource)
        {
            TokenService = tokenService;
            Context = context;
            Db = db;
            DataSource = dataSource;
        }

        public Gaos.Model.Token.TokenClaims GetTokenClaims()
        {
            const string METHOD_NAME = "GetTokenClaims()";
            Gaos.Model.Token.TokenClaims? claims;
            if (Context.Items.ContainsKey(Gaos.Common.Context.HTTP_CONTEXT_KEY_TOKEN_CLAIMS) == false)
            {
                Log.Error($"{CLASS_NAME}:{METHOD_NAME} no token claims");
                throw new Exception("no token claims");
            }
            else
            {
                claims = Context.Items[Gaos.Common.Context.HTTP_CONTEXT_KEY_TOKEN_CLAIMS] as Gaos.Model.Token.TokenClaims;
            }
            return claims;
        }

        public int GetUserId()
        {
            Gaos.Model.Token.TokenClaims claims = GetTokenClaims();
            return claims.UserId;

        }

        public Gaos.Dbo.Model.User GetUser()
        {
            const string METHOD_NAME = "GetUser()";
            if (User == null)
            {
                int userId = GetUserId();
                User = Db.User.FirstOrDefault(x => x.Id == userId);
                if (User == null)
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME} no such user");
                    throw new Exception("no such user");

                }
                return User;
            }
            else
            {
                return User;
            }
        }

        public string GetCountry()
        {
            const string METHOD_NAME = "GetCountry()";
            if (User == null)
            {
                int userId = GetUserId();
                User = Db.User.FirstOrDefault(x => x.Id == userId);
                if (User == null)
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME} no such user");
                    throw new Exception("no such user");
                }
                return User.Country;
            }
            else
            {
                return User.Country;
            }
        }

        public string GetLanguage()
        {
            const string METHOD_NAME = "GetLanguage()";
            if (User == null)
            {
                int userId = GetUserId();
                User = Db.User.FirstOrDefault(x => x.Id == userId);
                if (User == null)
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME} no such user");
                    throw new Exception("no such user");
                }
                return User.Language;
            }
            else
            {
                return User.Language;
            }
        }

        public (Gaos.Dbo.Model.User?, Gaos.Dbo.Model.JWT?) GetDeviceUser(int deviceId)
        {
            const string METHOD_NAME = "GetDeviceUser()";
            Gaos.Dbo.Model.User? user = null;

            Gaos.Dbo.Model.JWT? jwt = Db.JWT.FirstOrDefault(x => x.DeviceId == deviceId);
            if (jwt != null)
            {
                user = Db.User.FirstOrDefault(x => x.Id == jwt.UserId);
                if (user == null)
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME} user not found for token");
                    throw new Exception("user not found for token");
                }
                if (user.IsGuest == null)
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME} corrupted user (isGuest is null), user.id {user.Id}");
                    throw new Exception("corrupted user (isGuest is null)");
                }
                var userType = (bool)user.IsGuest ? Gaos.Model.Token.UserType.GuestUser : Gaos.Model.Token.UserType.RegisteredUser;
                var jwtStr = TokenService.GenerateJWT(
                    user.Name, user.Id, deviceId,
                    DateTimeOffset.UtcNow.AddHours(Gaos.Common.Context.TOKEN_EXPIRATION_HOURS).ToUnixTimeSeconds(),
                    userType);
                jwt = Db.JWT.FirstOrDefault(x => x.DeviceId == deviceId);
                if (jwt != null)
                {
                    return (user, jwt);
                }
                else
                {
                    Log.Error($"{CLASS_NAME}:GetDeviceUser() no jwt");
                    throw new Exception("no jwt");
                }
            }
            else
            {
                return (user, jwt);
            }

        }

        private class GetUserGroup_query1_result
        {
            public bool IsGroupOwner { get; set; }
            public int GroupId { get; set; }
            public string GroupOwnerName { get; set; }
        }

        private class GetUserGroup_query2_result
        {
            public bool IsGroupMember { get; set; }
            public int GroupId { get; set; }
            public int GroupOwnerId { get; set; }
            public string GroupOwnerName { get; set; }
        }

        public class GetGroupResult
        {
            public bool IsGroupOwner { get; set; }
            public bool IsGroupMember { get; set; }
            public int GroupId { get; set; }
            public int GroupOwnerId { get; set; }
            public string GroupOwnerName { get; set; }
        }


        // Returns either group of which user is owner or group of which user is member or null if user is neither owner nor member of any group.
        // If group does not have any members it is not considered to be a group, user is not considered to be an owner.

        public async Task<GetGroupResult?> GetUserGroup_old()
        {
            const string METHOD_NAME = "GetGroup()";

            GetGroupResult result;
            try
            {

                if (this.getGroupResult != null)
                {
                    return this.getGroupResult;
                }

                var user = GetUser();
                if (user == null)
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME} user not logged in");
                    throw new Exception("user not logged int");
                }

                // search for Group entry where user is owner
                var query_1 = from g in Db.Groupp
                              join u in Db.User on g.OwnerId equals u.Id
                              where g.OwnerId == user.Id
                              select new GetUserGroup_query1_result
                              {
                                  IsGroupOwner = (g != null),
                                  GroupId = (g != null) ? g.Id : -1,
                                  GroupOwnerName = (u != null) ? u.Name : "",
                              };
                var queryResult_1 = await query_1.FirstOrDefaultAsync();

                if (queryResult_1 != null)
                {
                    result = new GetGroupResult
                    {
                        IsGroupOwner = queryResult_1.IsGroupOwner,
                        IsGroupMember = false,
                        GroupId = queryResult_1.GroupId,
                        GroupOwnerId = user.Id,
                        GroupOwnerName = queryResult_1.GroupOwnerName,
                    };

                    if (result.IsGroupOwner)
                    {
                        // If user has a group the group has to have at least one member otherwise user is not considered be an owner
                        var query_3 = from gm in Db.GroupMember
                                      where gm.GroupId == result.GroupId
                                      select (gm != null);
                        var queryResult_3 = await query_3.FirstOrDefaultAsync();
                        if (queryResult_3)
                        {
                            this.getGroupResult = result;
                            return result;
                        }
                    }
                }

                {
                    // search for GroupMember entry where user is member
                    var query_2 = from gm in Db.GroupMember
                                  join g in Db.Groupp on gm.GroupId equals g.Id
                                  join u in Db.User on g.OwnerId equals u.Id
                                  where gm.UserId == user.Id
                                  select new GetUserGroup_query2_result
                                  {
                                      IsGroupMember = (gm != null),
                                      GroupId = (g != null) ? g.Id : -1,
                                      GroupOwnerId = (g != null) ? (int)g.OwnerId : -1,
                                      GroupOwnerName = (u != null) ? u.Name : "",
                                  };
                    var queryResult_2 = await query_2.FirstOrDefaultAsync();

                    if (queryResult_2 != null)
                    {
                        result = new GetGroupResult
                        {
                            IsGroupOwner = false,
                            IsGroupMember = queryResult_2.IsGroupMember,
                            GroupId = queryResult_2.GroupId,
                            GroupOwnerId = queryResult_2.GroupOwnerId,
                            GroupOwnerName = queryResult_2.GroupOwnerName,
                        };
                        this.getGroupResult = result;
                        return result;
                    }
                    else
                    {
                        // user is neither owner nor member of any group
                        this.getGroupResult = null;
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, $"{CLASS_NAME}:{METHOD_NAME} {e.Message}");
                throw new Exception($"getting user group failed");
            }
        }

        // Returns either group of which user is owner or group of which user is member or null if user is neither owner nor member of any group.
        // If group does not have any members it is not considered to be a group, user is not considered to be an owner.

        public async Task<GetGroupResult?> GetUserGroup()
        {
            const string METHOD_NAME = "GetUserGroup1()";
            const string sqlQueryMember =
$@"
SELECT 
    g.Id AS memberGroupId,
    g.OwnerId AS memberGroupOwnerId,
    u.Name AS memberGroupOwnerName
FROM Groupp g
JOIN GroupMember gm ON g.Id = gm.GroupId
JOIN User u ON g.OwnerId = u.Id
WHERE gm.UserId = @userId
";
            const string sqlQueryOwner =
$@"
SELECT 
    g.Id AS ownedGroupId,
    g.Name AS ownedGroupName
FROM Groupp g
WHERE g.OwnerId = @userId
    AND EXISTS (SELECT 1 FROM GroupMember gm WHERE gm.GroupId = g.Id)
";
            try
            {
                if (this.isGetGroupResult)
                {
                    return this.getGroupResult;
                }

                var user = GetUser();
                if (user == null)
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME} user not logged in");
                    throw new Exception("user not logged int");
                }

                // first try if user is a member of a group

                //await DbConn.OpenAsync();
                using var connection = await DataSource.OpenConnectionAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlQueryMember;
                    command.Parameters.AddWithValue("@userId", user.Id);
                    using var reader = await command.ExecuteReaderAsync();

                    bool isRow = await reader.ReadAsync();
                    if (isRow)
                    {
                        int memberGroupId;
                        int memberGroupOwnerId;
                        string memberGroupOwnerName;
                        {
                            memberGroupId = reader.GetInt32(0);
                            memberGroupOwnerId = reader.GetInt32(1);
                            memberGroupOwnerName = reader.GetString(2);
                        }
                        var getGroupResult = new GetGroupResult
                        {
                            IsGroupOwner = false,
                            IsGroupMember = true,
                            GroupId = memberGroupId,
                            GroupOwnerId = memberGroupOwnerId,
                            GroupOwnerName = memberGroupOwnerName
                        };
                        this.getGroupResult = getGroupResult;
                        this.isGetGroupResult = true;
                        reader.Close();
                        return getGroupResult;
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlQueryOwner;
                    command.Parameters.AddWithValue("@userId", user.Id);
                    using var reader = await command.ExecuteReaderAsync();

                    bool isRow = await reader.ReadAsync();
                    if (isRow)
                    {
                        int ownedGroupId = reader.GetInt32(0);
                        string ownedGroupName = reader.GetString(1); 
                        var getGroupResult = new GetGroupResult
                        {
                            IsGroupOwner = true,
                            IsGroupMember = false,
                            GroupId = ownedGroupId,
                            GroupOwnerId = user.Id,
                            GroupOwnerName = ownedGroupName
                        };
                        this.getGroupResult = getGroupResult;
                        this.isGetGroupResult = true;
                        reader.Close();
                        return getGroupResult;
                    }
                }


                return null;


            }
            catch (Exception e)
            {
                Log.Error(e, $"{CLASS_NAME}:{METHOD_NAME} {e.Message}");
                throw new Exception($"getting user group failed");
            }
        }

        public async Task<bool> IsUserInGroup(int groupId, int userId)
        {
            const string METHOD_NAME = "IsUserInGroup()";

            try
            {
                bool isInGroup =   await Db.GroupMember.Where(x => x.GroupId == groupId && x.UserId == userId).AnyAsync();
                if (!isInGroup)
                {
                    isInGroup = await Db.Groupp.Where(x => x.Id == groupId && x.OwnerId == userId).AnyAsync();
                }
                return isInGroup;
            }
            catch (Exception e)
            {
                Log.Error(e, $"{CLASS_NAME}:{METHOD_NAME} {e.Message}");
                throw new Exception($"getting user group failed");
            }
        }

        public async Task UpdateCountry(int userId, string country)
        {
            const string METHOD_NAME = "UpdateCountry()";

            try
            {
                var user = await Db.User.FirstOrDefaultAsync(x => x.Id == userId);

                if (user == null)
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME} user not found");
                    throw new Exception("no such user");
                }

                user.Country = country;
                await Db.SaveChangesAsync();

                Log.Information($"{CLASS_NAME}:{METHOD_NAME} updated country for user {userId} to {country}");
            }
            catch (Exception e)
            {
                Log.Error(e, $"{CLASS_NAME}:{METHOD_NAME} failed to update country for user {userId}");
                throw new Exception("failed to update country");
            }
        }

        public async Task UpdateLanguage(int userId, string language)
        {
            const string METHOD_NAME = "UpdateLanguage()";

            try
            {
                var user = await Db.User.FirstOrDefaultAsync(x => x.Id == userId);

                if (user == null)
                {
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME} user not found");
                    throw new Exception("no such user");
                }

                user.Language = language;
                await Db.SaveChangesAsync();

                Log.Information($"{CLASS_NAME}:{METHOD_NAME} updated language for user {userId} to {language}");
            }
            catch (Exception e)
            {
                Log.Error(e, $"{CLASS_NAME}:{METHOD_NAME} failed to update language for user {userId}");
                throw new Exception("failed to update language");
            }
        }

        public async Task UpdateUserColors(int userId, UserInterfaceColors colors)
        {
            const string METHOD_NAME = "UpdateUserColors()";

            try
            {
                var userColors = await Db.UserInterfaceColors.FirstOrDefaultAsync(x => x.UserId == userId);

                if (userColors == null)
                {
                    userColors = colors;
                    await Db.UserInterfaceColors.AddAsync(userColors);
                }
                else
                {
                    userColors = colors;
                }

                await Db.SaveChangesAsync();

                Log.Information($"{CLASS_NAME}:{METHOD_NAME} updated colors for user {userId}");
            }
            catch (Exception e)
            {
                Log.Error(e, $"{CLASS_NAME}:{METHOD_NAME} failed to update colors for user {userId}");
                throw new Exception("failed to update user colors");
            }
        }
    }
}
