using Microsoft.EntityFrameworkCore;

namespace Gaos.Seed
{
    public class Role
    {
        public static void Seed(ModelBuilder modelBuilder, IConfiguration configuration, IWebHostEnvironment environment)
        {
            if (!environment.IsProduction())
            {
                // seed Dbo.User
                modelBuilder.Entity<Gaos.Dbo.Model.Role>().HasData(
                    new Gaos.Dbo.Model.User { Id = Gaos.Common.Context.ROLE_PLAYER_ID, Name = Gaos.Common.Context.ROLE_PLAYER_NAME},
                    new Gaos.Dbo.Model.User { Id = Gaos.Common.Context.ROLE_ADMIN_ID, Name = Gaos.Common.Context.ROLE_ADMIN_NAME}
                );
            }
        } 
    }
}
