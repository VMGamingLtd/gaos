using Microsoft.EntityFrameworkCore;

namespace Gaos.Seed
{
    public class JWT
    {
        public static void Seed(ModelBuilder modelBuilder, IConfiguration configuration, IWebHostEnvironment environment)
        {
            if (!environment.IsProduction())
            {
                // seed Dbo.User
                modelBuilder.Entity<Gaos.Dbo.Model.JWT>().HasData(
                    new Gaos.Dbo.Model.JWT { Id = 1, Token = "x", UserId = 1 },
                    new Gaos.Dbo.Model.JWT { Id = 2, Token = "x", UserId = 1 },
                    new Gaos.Dbo.Model.JWT { Id = 3, Token = "x", UserId = 2 },
                    new Gaos.Dbo.Model.JWT { Id = 4, Token = "x", UserId = 2 },
                    new Gaos.Dbo.Model.JWT { Id = 5, Token = "x", UserId = 3 },
                    new Gaos.Dbo.Model.JWT { Id = 6, Token = "x", UserId = 3 },
                    new Gaos.Dbo.Model.JWT { Id = 7, Token = "x", UserId = 3 }
                );
            }
        }
    }
}
