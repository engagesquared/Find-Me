// <copyright file="FindMeDbContext.cs" company="Engage Squared">
// Copyright (c) Engage Squared. All rights reserved.
// </copyright>

namespace FindMe.Core.DB
{
    using FindMe.Core.DB.Entities;
    using Microsoft.EntityFrameworkCore;

    public class FindMeDbContext : DbContext
    {
        private static bool isEnsured;

        public FindMeDbContext(DbContextOptions<FindMeDbContext> options)
        : base(options)
        {
            // hack to ensure once per application run, becauser Azure Function DI doesn't provide such option.
            if (!isEnsured)
            {
                this.Database.EnsureCreated();
                isEnsured = true;
            }
        }

        public DbSet<UserEntity> Users { get; protected set; }

        public DbSet<UserConversationReferenceEntity> ConverstaionReferences { get; protected set; }

        public DbSet<WeekScheduleEntity> WeekSchedules { get; protected set; }

        public DbSet<ConfigEntity> Config { get; protected set; }

        public DbSet<UserStatusEntity> UserStatuses { get; protected set; }

        public DbSet<StatusEntity> Statuses { get; protected set; }

        public DbSet<LocationEntity> Locations { get; protected set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserEntity>(e =>
            {
                e.Property(x => x.AadUserId).IsRequired();
                e.Property(x => x.BotUserId).HasMaxLength(500);
                e.Property(x => x.ManagerId);
                e.Property(x => x.JobTitle);
                e.Property(x => x.Name).HasMaxLength(50);
                e.Property(x => x.Email).HasMaxLength(50);
                e.Property(x => x.EmailNamePart).HasMaxLength(40);
                e.Property(x => x.ManagerId);
                e.Property(x => x.ManagerIsEmpty);
                e.Property(x => x.PhonePersonal).HasMaxLength(50);
                e.Property(x => x.NextKinName).HasMaxLength(50);
                e.Property(x => x.NextKinRelation).HasMaxLength(50);
                e.Property(x => x.NextKinPhone).HasMaxLength(50);
                e.HasKey(x => x.AadUserId);
                e.HasOne(x => x.Manager).WithMany(x => x.Reporters).HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.NoAction);
                e.HasMany(x => x.Statuses).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
                e.HasMany(x => x.WeekSchedules).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<UserConversationReferenceEntity>(e =>
            {
                e.Property(e => e.UserId).IsRequired();
                e.Property(e => e.ConversationId).HasMaxLength(500).IsRequired();
                e.HasOne(x => x.User).WithOne().HasForeignKey<UserConversationReferenceEntity>(x => x.UserId);
                e.HasKey(x => x.UserId);
            });

            modelBuilder.Entity<WeekScheduleEntity>(e =>
            {
                e.Property(e => e.Id).ValueGeneratedOnAdd();
                e.Property(e => e.UserId).IsRequired();
                e.Property(e => e.StartDateUtc).IsRequired();
                e.Property(e => e.StartDate).IsRequired();

                e.Property(e => e.MondayStartTime);
                e.Property(e => e.MondayEndTime);
                e.Property(e => e.TuesdayStartTime);
                e.Property(e => e.TuesdayEndTime);
                e.Property(e => e.WednesdayStartTime);
                e.Property(e => e.WednesdayEndTime);
                e.Property(e => e.ThursdayStartTime);
                e.Property(e => e.ThursdayEndTime);
                e.Property(e => e.FridayStartTime);
                e.Property(e => e.FridayEndTime);
                e.Property(e => e.SaturdayStartTime);
                e.Property(e => e.SaturdayEndTime);
                e.Property(e => e.SundayStartTime);
                e.Property(e => e.SundayEndTime);

                e.HasKey(x => x.Id);
                e.HasOne(x => x.User).WithMany(x => x.WeekSchedules).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<ConfigEntity>(e =>
            {
                e.Property(e => e.Key).HasMaxLength(25);
                e.Property(e => e.Value);
                e.HasKey(x => x.Key);
            });

            modelBuilder.Entity<StatusEntity>(e =>
            {
                e.Property(e => e.Id).ValueGeneratedOnAdd();
                e.Property(e => e.Title).HasMaxLength(500);
                e.Property(e => e.Order);
                e.HasKey(x => x.Id);
            });

            modelBuilder.Entity<LocationEntity>(e =>
            {
                e.Property(e => e.Id).ValueGeneratedOnAdd();
                e.Property(e => e.Address).HasMaxLength(500);
                e.Property(e => e.Phone).HasMaxLength(100);
                e.HasKey(x => x.Id);
            });

            modelBuilder.Entity<UserStatusEntity>(e =>
            {
                e.Property(e => e.Id).ValueGeneratedOnAdd();
                e.Property(e => e.UserId).IsRequired();
                e.Property(e => e.Type);
                e.Property(e => e.StatusId);
                e.Property(e => e.OtherStatus).HasMaxLength(500);
                e.Property(e => e.LocationId);
                e.Property(e => e.Comments);
                e.Property(e => e.Created);
                e.Property(e => e.CreatedById).IsRequired();
                e.Property(e => e.Expired);
                e.HasKey(x => x.Id);
                e.HasOne(x => x.User).WithMany(x => x.Statuses).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(x => x.Status).WithMany().HasForeignKey(x => x.StatusId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.NoAction);
            });

            // Seed data
            modelBuilder.Entity<StatusEntity>().HasData(
                new StatusEntity { Id = 1, Order = 1, Title = "Community visit" });
        }
    }
}
