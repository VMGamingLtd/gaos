using Serilog;
namespace Gaos.Dbo
{
    using Microsoft.EntityFrameworkCore;
    using System.Xml;
    using Gaos.Dbo.Model;
    using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

    public class Db : DbContext, IDataProtectionKeyContext
    {
        private IConfiguration Configuration;
        private IWebHostEnvironment Environment;
        //public Db(DbContextOptions<Db> options) : base(options) { }
        public Db(DbContextOptions<Db> options, IConfiguration configuration, IWebHostEnvironment environment) : base(options) 
        { 
            this.Configuration = configuration;
            this.Environment = environment;
        }

        public DbSet<User> User => Set<User>();
        public DbSet<UserEmail> UserEmail => Set<UserEmail>();
        public DbSet<UserVerificationCode> UserVerificationCode => Set<UserVerificationCode>();
        public DbSet<Role> Role => Set<Role>();
        public DbSet<UserRole> UserRole => Set<UserRole>();
        public DbSet<JWT> JWT => Set<JWT>();
        public DbSet<BuildVersion> BuildVersion => Set<BuildVersion>();
        public DbSet<Device> Device => Set<Device>();
        public DbSet<Session> Session => Set<Session>();
        public DbSet<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey> DataProtectionKeys => Set<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey>();
        public DbSet<ChatRoom> ChatRoom => Set<ChatRoom>();
        public DbSet<ChatRoomMember> ChatRoomMember => Set<ChatRoomMember>();
        public DbSet<ChatRoomMessage> ChatRoomMessage => Set<ChatRoomMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            // User
            modelBuilder.Entity<User>().HasKey(e => e.Id);
            modelBuilder.Entity<User>().HasIndex(e => e.Name).IsUnique(true);
            modelBuilder.Entity<User>().HasIndex(e => e.Email).IsUnique(true);
            modelBuilder.Entity<User>().HasOne(e => e.Device).WithMany().HasForeignKey(e => e.DeviceId);
            modelBuilder.Entity<User>().HasIndex(e => e.EmailVerificationCode).IsUnique(true);

            // UserEmail
            modelBuilder.Entity<UserEmail>().HasKey(e => e.Id);
            modelBuilder.Entity<UserEmail>().HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            modelBuilder.Entity<UserEmail>().HasIndex(e => e.EmailVerificationCode).IsUnique(true);
            modelBuilder.Entity<UserEmail>().HasIndex(e => e.Email).IsUnique(true);

            // UserVerificationCode
            modelBuilder.Entity<UserVerificationCode>().HasKey(e => e.Id);
            modelBuilder.Entity<UserVerificationCode>().HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            modelBuilder.Entity<UserVerificationCode>().HasIndex(e => e.Code).IsUnique(true);


            // Role
            modelBuilder.Entity<Role>().HasKey(e => e.Id);
            modelBuilder.Entity<Role>().HasIndex(e => e.Name).IsUnique(true);

            // UserRole
            modelBuilder.Entity<UserRole>().HasKey(e => e.Id);
            modelBuilder.Entity<UserRole>().HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            modelBuilder.Entity<UserRole>().HasOne(e => e.Role).WithMany().HasForeignKey(e => e.RoleId);
            modelBuilder.Entity<UserRole>().HasIndex(e => new { e.UserId, e.RoleId }).IsUnique(true);

            // JWT
            modelBuilder.Entity<JWT>().HasKey(e => e.Id);
            modelBuilder.Entity<JWT>().HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            modelBuilder.Entity<JWT>().HasOne(e => e.Device).WithMany().HasForeignKey(e => e.DeviceId);

            // BuildVersion
            modelBuilder.Entity<BuildVersion>().HasKey(e => e.Id);


            // Device
            modelBuilder.Entity<Device>().HasKey(e => e.Id);
            modelBuilder.Entity<Device>()
                .HasIndex(e => new { e.Identification, e.PlatformType }).IsUnique(true);
            modelBuilder.Entity<Device>().HasOne(e => e.BuildVersion).WithMany().HasForeignKey(e => e.BuildVersionId);

            // Session
            modelBuilder.Entity<Session>().HasKey(e => e.Id);

            // DataProtectionKey
            modelBuilder.Entity<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey>().HasKey(e => e.Id);


            // ChatRoom
            modelBuilder.Entity<ChatRoom>().HasKey(e => e.Id);
            modelBuilder.Entity<ChatRoom>().HasOne(e => e.Owner).WithMany().HasForeignKey(e => e.OwnerId);

            // ChatRoomMember
            modelBuilder.Entity<ChatRoomMember>().HasKey(e => e.Id);
            modelBuilder.Entity<ChatRoomMember>().HasOne(e => e.ChatRoom).WithMany().HasForeignKey(e => e.ChatRoomId);
            modelBuilder.Entity<ChatRoomMember>().HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            modelBuilder.Entity<ChatRoomMember>().HasIndex(e => new { e.ChatRoomId, e.UserId }).IsUnique(true);

            // ChatRoomMessage
            modelBuilder.Entity<ChatRoomMessage>().HasKey(e => e.Id);
            modelBuilder.Entity<ChatRoomMessage>().HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            modelBuilder.Entity<ChatRoomMessage>().HasOne(e => e.ChatRoom).WithMany().HasForeignKey(e => e.ChatRoomId);
            modelBuilder.Entity<ChatRoomMessage>().HasIndex(e => new { e.ChatRoomId, e.MessageId }).IsUnique(true);

            Gaos.Seed.SeedAll.Seed(modelBuilder, Configuration, Environment);
        }
    }
}
