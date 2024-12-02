using Gaos.Routes.Model.WebsiteDataJson;
using Gaos.Dbo;
using Gaos.Dbo.Model;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;
using MySqlConnector;

namespace Gaos.Common
{
    public class WebsiteService
    {
        private static string CLASS_NAME = typeof(WebsiteService).Name;
        private Gaos.Dbo.Db Db;
        private MySqlDataSource dataSource; 

        public WebsiteService(Gaos.Dbo.Db db, MySqlDataSource dataSource)
        {
            Db = db;
            this.dataSource = dataSource;
        }

        public async Task<LeaderboardDataGetResponse> GetLeaderboardDataAsync()
        {
            const string METHOD_NAME = "GetLeaderboardDataAsync()";

            try
            {
                List<LeaderboardData> leaderboardEntries = await Db.LeaderboardData.Where(x => x.Score > 0)
                                                                                   .OrderByDescending(x => x.Score)
                                                                                   .Take(20)
                                                                                   .ToListAsync();
                LeaderboardDataGetResponse response = new();

                if (leaderboardEntries.Count > 0)
                {
                    string jsonString = JsonConvert.SerializeObject(leaderboardEntries, Formatting.Indented);
                    response.Error = false;
                    response.ErrorMessage = "";
                    response.LeaderboardDataJson = jsonString;
                }
                else
                {
                    response.Error = true;
                    response.ErrorMessage = "No data found";
                }

                return response;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                throw new Exception("failed to get Leaderboard data");
            }
        }

        public async Task UpdateNewsData(NewsData data)
        {
            const string METHOD_NAME = "UpdateNewsData()";

            try
            {
                await Db.NewsData.AddAsync(data);
                await Db.SaveChangesAsync();

                Log.Information($"{CLASS_NAME}:{METHOD_NAME} updated Website News data with Id {data.Id}");
            }
            catch (Exception e)
            {
                Log.Error(e, $"{CLASS_NAME}:{METHOD_NAME} failed to update News data with Id {data.Id}");
                throw new Exception("failed to update News data with Id {data.Id}");
            }
        }

        public async Task<NewsDataGetResponse> GetNewsDataAsync()
        {
            const string METHOD_NAME = "GetNewsDataAsync()";

            try
            {
                List<NewsData> newsEntries = await Db.NewsData.OrderByDescending(x => x.Id)
                                                              .ToListAsync();
                NewsDataGetResponse response = new();

                if (newsEntries.Count > 0)
                {
                    string jsonString = JsonConvert.SerializeObject(newsEntries, Formatting.Indented);
                    response.Error = false;
                    response.ErrorMessage = "";
                    response.NewsDataJson = jsonString;
                }
                else
                {
                    response.Error = true;
                    response.ErrorMessage = "No data found";
                }

                return response;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                throw new Exception("failed to get Leaderboard data");
            }
        }

        public async Task UpdateLeaderboardData(LeaderboardData data)
        {
            const string METHOD_NAME = "UpdateLeaderboardData()";

            try
            {
                var userData = await Db.LeaderboardData.FirstOrDefaultAsync(x => x.UserId == data.UserId);

                if (userData == null)
                {
                    userData = data;
                    await Db.LeaderboardData.AddAsync(userData);
                }
                else
                {
                    userData.Country = data.Country;
                    userData.Hours = data.Hours;
                    userData.Minutes = data.Minutes;
                    userData.Seconds = data.Seconds;
                    userData.Score = data.Score;
                }

                await Db.SaveChangesAsync();

                Log.Information($"{CLASS_NAME}:{METHOD_NAME} updated Leaderboard data for user {data.UserId}");
            }
            catch (Exception e)
            {
                Log.Error(e, $"{CLASS_NAME}:{METHOD_NAME} failed to update Leaderboard data for user {data.UserId}");
                throw new Exception("failed to update Leaderboard data");
            }
        }

        // UpdateLeaderboardData() alternative for comparing of using raw sql. 
        // Using raw SQL to update leaderboard data is 3x faster than using EF Core !!!!!
        public async Task UpdateLeaderboardData_1(LeaderboardData data)
        {
            const string METHOD_NAME = "UpdateLeaderboardData_1()";

            try
            {
                using var connection = await dataSource.OpenConnectionAsync();

                bool exists;
                {
                    using var command = connection.CreateCommand();
                    // check if updateLeaderboardData exists
                    command.CommandText = "SELECT COUNT(*) FROM LeaderboardData WHERE UserId = @UserId";
                    command.Parameters.AddWithValue("@UserId", data.UserId);
                    var result = await command.ExecuteScalarAsync();
                    if (result == null)
                    {
                        Log.Error($"{CLASS_NAME}:{METHOD_NAME}: Could not check if LeaderboardData exists: result is null");
                        throw new Exception("Could not check if LeaderboardData exists: result is null");
                    }
                    exists = (long)result > 0;

                }

                if (!exists) {
                    // insert new record
                    using var command = connection.CreateCommand();
                    command.CommandText = "INSERT INTO LeaderboardData (UserId, Username, Country, Hours, Minutes, Seconds, Score) VALUES (@UserId, @Username, @Country, @Hours, @Minutes, @Seconds, @Score)";
                    command.Parameters.AddWithValue("@UserId", data.UserId);
                    command.Parameters.AddWithValue("@Username", data.Username);
                    command.Parameters.AddWithValue("@Country", data.Country);
                    command.Parameters.AddWithValue("@Hours", data.Hours);
                    command.Parameters.AddWithValue("@Minutes", data.Minutes);
                    command.Parameters.AddWithValue("@Seconds", data.Seconds);
                    command.Parameters.AddWithValue("@Score", data.Score);
                    var result = await command.ExecuteNonQueryAsync();
                    if (result == 0)
                    {
                        Log.Error($"{CLASS_NAME}:{METHOD_NAME}: Could not insert LeaderboardData");
                        throw new Exception("Could not insert LeaderboardData");
                    }
                }
                else
                {
                    // update existing record
                    using var command = connection.CreateCommand();
                    command.CommandText = "UPDATE LeaderboardData SET Username = @Username, Country = @Country, Hours = @Hours, Minutes = @Minutes, Seconds = @Seconds, Score = @Score WHERE UserId = @UserId";
                    command.Parameters.AddWithValue("@UserId", data.UserId);
                    command.Parameters.AddWithValue("@Username", data.Username);
                    command.Parameters.AddWithValue("@Country", data.Country);
                    command.Parameters.AddWithValue("@Hours", data.Hours);
                    command.Parameters.AddWithValue("@Minutes", data.Minutes);
                    command.Parameters.AddWithValue("@Seconds", data.Seconds);
                    command.Parameters.AddWithValue("@Score", data.Score);
                    var result = await command.ExecuteNonQueryAsync();
                    if (result == 0)
                    {
                        Log.Error($"{CLASS_NAME}:{METHOD_NAME}: Could not update LeaderboardData");
                        throw new Exception("Could not update LeaderboardData");
                    }
                }

                Log.Information($"{CLASS_NAME}:{METHOD_NAME} updated Leaderboard data for user {data.UserId}");
            }
            catch (Exception e)
            {
                Log.Error(e, $"{CLASS_NAME}:{METHOD_NAME} failed to update Leaderboard data for user {data.UserId}");
                throw new Exception("failed to update Leaderboard data");
            }
        }
    }
}
