using System.Diagnostics;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using selflix.Services;
using Serilog;

namespace selflix.Db;

public class SqliteDbContext : AppDbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => AdditionalConfiguration(options.UseSqlite(C.Paths.AppDbConnectionString));
}
public class PostgresDbContext : AppDbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder options)
         => AdditionalConfiguration(options.UseNpgsql(C.PostgresConnectionString));
}
public partial class AppDbContext : DbContext, IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    public DbSet<Library> Libraries => Set<Library>();
    public DbSet<User> Users => Set<User>();

    protected void AdditionalConfiguration(DbContextOptionsBuilder options)
    {
        options.UseSnakeCaseNamingConvention();
        if (C.IsDebug)
        {
            options.EnableSensitiveDataLogging();
            options.LogTo(message => Debug.WriteLine(message), new[] { RelationalEventId.CommandExecuted });
        }
    }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Library>(e =>
        {
            e.HasKey(e => e.LibraryId);
            e.HasMany(e => e.Users).WithMany(e => e.Libraries);
            e.HasMany(e => e.Movies).WithOne(e => e.Library).HasForeignKey(e => e.LibraryId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(e => e.Series).WithOne(e => e.Library).HasForeignKey(e => e.LibraryId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Movie>(e =>
        {
            e.HasKey(e => e.MovieId);
            e.HasMany(e => e.Views).WithOne(e => e.Movie).HasForeignKey(e => e.MovieId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Serie>(e =>
        {
            e.HasKey(e => e.SerieId);
            e.HasMany(e => e.Episodes).WithOne(e => e.Serie).HasForeignKey(e => e.SerieId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SerieEpisode>(e =>
        {
            e.HasKey(e => e.SerieEpisodeId);
            e.HasMany(e => e.Views).WithOne(e => e.SerieEpisode).HasForeignKey(e => e.SerieEpisodeId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<User>(e =>
        {
            e.HasKey(e => e.UserId);
            e.HasMany(e => e.Devices).WithOne(e => e.User).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(e => e.Watchers).WithOne(e => e.User).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserDevice>(e =>
        {
            e.HasKey(e => e.UserDeviceId);
        });

        builder.Entity<Watcher>(e =>
        {
            e.HasKey(e => e.WatcherId);
            e.HasMany(e => e.Movies).WithOne(e => e.Watcher).HasForeignKey(e => e.WatcherId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(e => e.SerieEpisodes).WithOne(e => e.Watcher).HasForeignKey(e => e.WatcherId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WatcherMovie>(e =>
        {
            e.HasKey(e => new { e.WatcherId, e.MovieId });
        });

        builder.Entity<WatcherSerieEpisode>(e =>
        {
            e.HasKey(e => new { e.WatcherId, e.SerieEpisodeId });
        });

        // SQLite conversions
        if (Database.IsSqlite())
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var dtProperties = entityType.ClrType.GetProperties()
                    .Where(p => p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?));
                foreach (var property in dtProperties)
                    builder.Entity(entityType.Name).Property(property.Name).HasConversion(new DateTimeToBinaryConverter());

                var decProperties = entityType.ClrType.GetProperties()
                    .Where(p => p.PropertyType == typeof(decimal) || p.PropertyType == typeof(decimal?));
                foreach (var property in decProperties)
                    builder.Entity(entityType.Name).Property(property.Name).HasConversion<double>();

                var spanProperties = entityType.ClrType.GetProperties()
                    .Where(p => p.PropertyType == typeof(TimeSpan) || p.PropertyType == typeof(TimeSpan?));
                foreach (var property in spanProperties)
                    builder.Entity(entityType.Name).Property(property.Name).HasConversion<long>();
            }
    }
    public async ValueTask InitializeDefaults(IPasswordHasher hasher)
    {
        var adminPass = IPasswordHasher.GeneratePassword(16);
        var adminHash = hasher.HashPassword(adminPass);
        var adminUser = new User("admin", adminHash, true);
        Users.Add(adminUser);
        await SaveChangesAsync();

        if (!Debugger.IsAttached)
        {
            Log.Warning("Generated default admin password {Password}, please disable the user after initial login", adminPass);
            return;
        }

        var devHash = hasher.HashPassword("dev");
        var devUser = new User("dev", devHash, true);
        Users.Add(devUser);
        await SaveChangesAsync();
    }
}