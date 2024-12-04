using AIHackathon.Model;
using Microsoft.EntityFrameworkCore;
using OneBot.Attributes;
using OneBot.Base;
using OneBot.Tg;

namespace AIHackathon
{
    [ServiceDB]
    public class DataBase : UsersDB<User>, IDBTg<User>
    {
        public DbSet<MetricsUser> Metrics { get; set; } = null!;
        public DbSet<Command> Commands { get; set; } = null!;

        public DbSet<TgUser<User>> TgUsers { get; set; } = null!;

        public DataBase(DbContextOptions options) : base(options)
        {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ConfigurateDBTg<User>();
            modelBuilder.Entity<Command>()
                .Property(p => p.Id)
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<MetricsUser>()
                .Property(p => p.MetricId)
                .ValueGeneratedOnAdd();
        }
    }
}
