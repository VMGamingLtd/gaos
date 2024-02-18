using Microsoft.EntityFrameworkCore;
using Gaos.Auth;

namespace Gaos.Seed
{
    public class User
    {
        public static int UserId = 0;
        public static int RoleId = 0;

        public static void CreateUser(ModelBuilder modelBuilder, Gaos.Dbo.Model.User user, int roleId)
        {
            modelBuilder.Entity<Gaos.Dbo.Model.User>().HasData(user);
            modelBuilder.Entity<Gaos.Dbo.Model.UserRole>().HasData(new Gaos.Dbo.Model.UserRole {Id = ++RoleId,  UserId = user.Id, RoleId = roleId });
        }
           


        public static void Seed(ModelBuilder modelBuilder, IConfiguration configuration, IWebHostEnvironment environment)
        {
            if (!environment.IsProduction())
            {
                UserId = 0;
                RoleId = 0;

                EncodedPassword password = Password.getEncodedPassword("karol");


                // seed Dbo.User

                CreateUser(
                    modelBuilder,
                    new Gaos.Dbo.Model.User { Id = ++UserId, Name = "admin", Email = "admin@localhost", PasswordHash = password.PasswordHash, PasswordSalt = password.PasswordSalt },
                    Gaos.Common.Context.ROLE_ADMIN_ID
                );
                CreateUser(
                    modelBuilder,
                    new Gaos.Dbo.Model.User { Id = ++UserId, Name = "karol1", Email = "karol1@localhost", PasswordHash = password.PasswordHash, PasswordSalt = password.PasswordSalt },
                    Gaos.Common.Context.ROLE_PLAYER_ID
                );
                CreateUser(
                    modelBuilder,
                    new Gaos.Dbo.Model.User { Id = ++UserId, Name = "karol2", Email = "karol2@localhost", PasswordHash = password.PasswordHash, PasswordSalt = password.PasswordSalt },
                    Gaos.Common.Context.ROLE_PLAYER_ID
                );
                CreateUser(
                    modelBuilder,
                    new Gaos.Dbo.Model.User { Id = ++UserId, Name = "karol3", Email = "karol3@localhost", PasswordHash = password.PasswordHash, PasswordSalt = password.PasswordSalt},
                    Gaos.Common.Context.ROLE_PLAYER_ID
                );

                string[] randomNames = GetRandomLastNames();
                Gaos.Dbo.Model.User[] users = new Gaos.Dbo.Model.User[randomNames.Length];

                for (int i = 0; i < randomNames.Length; i++)
                {
                    var user = new Gaos.Dbo.Model.User
                    {
                        Id = ++UserId,
                        Name = $"{randomNames[i]}",
                        Email = $"{randomNames[i]}",
                        PasswordHash = password.PasswordHash,
                        PasswordSalt = password.PasswordSalt,
                    };
                    CreateUser(modelBuilder, user, Gaos.Common.Context.ROLE_PLAYER_ID);
                }
            }
        } 

        public static string[] GetRandomLastNames()
        {

            return new string[] {
                "Abbott",
                "Adams",
                "Anderson",
                "Baker",
                "Bennett",
                "Bishop",
                "Campbell",
                "Carlson",
                "Carter",
                "Chambers",
                "Collins",
                "Davidson",
                "Davis",
                "Dixon",
                "Donovan",
                "Douglas",
                "Edwards",
                "Ellis",
                "Evans",
                "Fisher",
                "Fitzgerald",
                "Foster",
                "Franklin",
                "Garcia",
                "Gibson",
                "Gonzalez",
                "Grayson",
                "Harrison",
                "Hawkins",
                "Hernandez",
                "Hughes",
                "Ingram",
                "Jackson",
                "Jameson",
                "Jenkins",
                "Johnson",
                "Kelly",
                "King",
                "Knight",
                "Lawrence",
                "Lawson",
                "Lewis",
                "Martinez",
                "Mitchell",
                "Morgan",
                "Morrison",
                "Nelson",
                "Olson",
                "Owens",
                "Palmer",
                "Parker",
                "Peterson",
                "Ramirez",
                "Reynolds",
                "Richardson",
                "Sanchez",
                "Saunders",
                "Scott",
                "Simmons",
                "Sullivan",
                "Thornton",
                "Turner",
                "Walker",
                "Wallace",
                "Watson",
                "Williams",
                "Wilson",
                "Yates",
                "Young",
                "Younger",
                "Zimmerman",
                };
        }
    }
}
