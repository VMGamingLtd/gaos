﻿using Microsoft.EntityFrameworkCore;

namespace Gaos.Seed
{
    public class SeedAll
    {
        public static void Seed(ModelBuilder modelBuilder, IConfiguration configuration, IWebHostEnvironment environment)
        {
            Role.Seed(modelBuilder, configuration, environment);
            User.Seed(modelBuilder, configuration, environment);
            JWT.Seed(modelBuilder, configuration, environment);
            Leaderboard.Seed(modelBuilder, configuration, environment);
            News.Seed(modelBuilder, configuration, environment);
        }
    }
}
