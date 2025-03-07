﻿namespace Gaos.Dbo
{
    using Gaos.Dbo.Model;
    using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;

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
        public DbSet<UserFriend> UserFriend => Set<UserFriend>();
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
        public DbSet<Groupp> Groupp => Set<Groupp>();
        public DbSet<GroupMember> GroupMember => Set<GroupMember>();
        public DbSet<GroupMemberRequest> GroupMemberRequest => Set<GroupMemberRequest>();
        public DbSet<UserInterfaceColors> UserInterfaceColors => Set<UserInterfaceColors>();
        public DbSet<GroupCredits> GroupCredits => Set<GroupCredits>();
        public DbSet<LeaderboardData> LeaderboardData => Set<LeaderboardData>();
        public DbSet<NewsData> NewsData => Set<NewsData>();

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

            // UserFriend
            modelBuilder.Entity<UserFriend>().HasKey(e => e.Id);
            modelBuilder.Entity<UserFriend>().HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            modelBuilder.Entity<UserFriend>().HasOne(e => e.Friend).WithMany().HasForeignKey(e => e.FriendId);
            modelBuilder.Entity<UserFriend>().HasIndex(e => new { e.UserId, e.FriendId }).IsUnique(true);

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

            // User Interface Colors
            modelBuilder.Entity<UserInterfaceColors>().HasKey(e => e.Id);
            modelBuilder.Entity<UserInterfaceColors>().HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);

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

            // Group
            modelBuilder.Entity<Groupp>().HasKey(e => e.Id);
            modelBuilder.Entity<Groupp>().HasOne(e => e.Owner).WithMany().HasForeignKey(e => e.OwnerId);
            modelBuilder.Entity<Groupp>().HasIndex(e => e.OwnerId).IsUnique();

            // GroupMember
            modelBuilder.Entity<GroupMember>().HasKey(e => e.Id);
            modelBuilder.Entity<GroupMember>().HasOne(e => e.Group).WithMany().HasForeignKey(e => e.GroupId);
            modelBuilder.Entity<GroupMember>().HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            modelBuilder.Entity<GroupMember>().HasIndex(e => new { e.GroupId, e.UserId }).IsUnique(true);

            // GroupMemberRequest
            modelBuilder.Entity<GroupMemberRequest>().HasKey(e => e.Id);
            modelBuilder.Entity<GroupMemberRequest>().HasOne(e => e.Group).WithMany().HasForeignKey(e => e.GroupId);
            modelBuilder.Entity<GroupMemberRequest>().HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            modelBuilder.Entity<GroupMemberRequest>().HasIndex(e => new { e.GroupId, e.UserId }).IsUnique(true);

            // GroupCredits
            modelBuilder.Entity<GroupCredits>().HasKey(e => e.Id);
            modelBuilder.Entity<GroupCredits>().HasOne(e => e.Group).WithMany().HasForeignKey(e => e.GroupId);
            modelBuilder.Entity<GroupCredits>().HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            modelBuilder.Entity<GroupMember>().HasIndex(e => new { e.GroupId, e.UserId }).IsUnique(true);

            // LeaderboardData
            modelBuilder.Entity<LeaderboardData>().HasKey(e => e.Id);
            modelBuilder.Entity<LeaderboardData>().HasIndex(e => e.UserId).IsUnique(true);

            // Website News data
            modelBuilder.Entity<NewsData>().HasKey(e => e.Id);

            Gaos.Seed.SeedAll.Seed(modelBuilder, Configuration, Environment);
        }
    }
}
