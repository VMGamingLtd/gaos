﻿using Gaos.Dbo;
using Microsoft.EntityFrameworkCore;

namespace Gaos.Seed
{
    public class SeedAll
    {
        public static void Seed(ModelBuilder modelBuilder, IConfiguration configuration, IWebHostEnvironment environment)
        {
            User.Seed(modelBuilder, configuration, environment);
            JWT.Seed(modelBuilder, configuration, environment);
        }
    }
}
