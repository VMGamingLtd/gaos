using Microsoft.EntityFrameworkCore;

namespace Gaos.Seed
{
    public class Leaderboard
    {
        public static void Seed(ModelBuilder modelBuilder, IConfiguration configuration, IWebHostEnvironment environment)
        {
            if (!environment.IsProduction())
            {
                // seed Dbo.Api1.Leaderboard
                modelBuilder.Entity<Gaos.Dbo.Model.LeaderboardData>().HasData(
                    new Gaos.Dbo.Model.LeaderboardData { Id = 1, UserId = 1, Username = "Gweba", Country = "Angola", Hours = 1, Minutes = 12, Seconds = 42, Score = 13 },
                    new Gaos.Dbo.Model.LeaderboardData { Id = 2, UserId = 2, Username = "Mia", Country = "Romania", Hours = 6, Minutes = 8, Seconds = 1, Score = 42 },
                    new Gaos.Dbo.Model.LeaderboardData { Id = 3, UserId = 3, Username = "Quin", Country = "UnitedKingdom", Hours = 2, Minutes = 46, Seconds = 11, Score = 32 },
                    new Gaos.Dbo.Model.LeaderboardData { Id = 4, UserId = 4, Username = "Larry", Country = "UnitedStates", Hours = 12, Minutes = 52, Seconds = 32, Score = 81 },
                    new Gaos.Dbo.Model.LeaderboardData { Id = 5, UserId = 5, Username = "Peter", Country = "Poland", Hours = 18, Minutes = 2, Seconds = 18, Score = 28 },
                    new Gaos.Dbo.Model.LeaderboardData { Id = 6, UserId = 6, Username = "Jozef", Country = "Slovakia", Hours = 11, Minutes = 9, Seconds = 27, Score = 65 },
                    new Gaos.Dbo.Model.LeaderboardData { Id = 7, UserId = 7, Username = "Ulrike", Country = "Austria", Hours = 0, Minutes = 24, Seconds = 12, Score = 8 },
                    new Gaos.Dbo.Model.LeaderboardData { Id = 8, UserId = 8, Username = "Hans", Country = "UnitedKingdom", Hours = 1, Minutes = 49, Seconds = 34, Score = 28 },
                    new Gaos.Dbo.Model.LeaderboardData { Id = 9, UserId = 9, Username = "Jose", Country = "Spain", Hours = 128, Minutes = 51, Seconds = 7, Score = 196 },
                    new Gaos.Dbo.Model.LeaderboardData { Id = 10, UserId = 10, Username = "Lee", Country = "China", Hours = 1024, Minutes = 1, Seconds = 52, Score = 689 }
                );
            }
        }
    }
}
