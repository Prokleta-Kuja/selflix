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
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();
    public DbSet<Watcher> Watchers => Set<Watcher>();
    public DbSet<WatcherVideo> WatcherVideos => Set<WatcherVideo>();

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
            e.HasMany(e => e.Videos).WithOne(e => e.Library).HasForeignKey(e => e.LibraryId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Video>(e =>
        {
            e.HasKey(e => e.VideoId);
            e.HasMany(e => e.Views).WithOne(e => e.Video).HasForeignKey(e => e.VideoId).OnDelete(DeleteBehavior.Cascade);
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
            e.HasMany(e => e.Videos).WithOne(e => e.Watcher).HasForeignKey(e => e.WatcherId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WatcherVideo>(e =>
        {
            e.HasKey(e => new { e.WatcherId, e.VideoId });
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
        if (!Debugger.IsAttached)
        {
            var adminPass = IPasswordHasher.GeneratePassword(16);
            var adminHash = hasher.HashPassword(adminPass);
            var adminUser = new User("admin", adminHash, true);
            Users.Add(adminUser);
            await SaveChangesAsync();
            Log.Warning("Generated default admin password {Password}, please disable the user after initial login", adminPass);
            return;
        }

        var devHash = hasher.HashPassword("dev");
        var devUser = new User("dev", devHash, true);

        var usrHash = hasher.HashPassword("usr");
        var usrUser = new User("usr", usrHash, true);

        Users.AddRange(devUser, usrUser);
        await SaveChangesAsync();

        var firstLibrary = new Library("First", "first");

        Libraries.AddRange(firstLibrary);
        await SaveChangesAsync();

        devUser.Libraries.Add(firstLibrary);
        usrUser.Libraries.Add(firstLibrary);

        var uno = new Watcher("Uno");
        var dos = new Watcher("Dos");
        usrUser.Watchers.Add(uno); usrUser.Watchers.Add(dos);

        var tv = new UserDevice("guid or something")
        {
            Brand = "Google",
            Model = "Chromecast HD",
            OS = "Android 11",
            UserId = usrUser.UserId,
        };
        UserDevices.Add(tv);
        await SaveChangesAsync();

        var libPath = C.Paths.MediaDataFor(firstLibrary.MediaPath);
        var files = new string[]{
            "Video Without Info/NoInfoVideo.AVI",
            "Video in a dir/DirVideo.mkv",
            "Video in a dir/DirVideo.nfo",
            "Video in a dir2/Video in a dir1/Dir2Video.mkv",
            "Video in a dir2/Video in a dir1/Dir2Video.nfo",
            "Video in a dir3/Video in a dir2/Video in a dir1/Dir3Video.mkv",
            "Video in a dir3/Video in a dir2/Video in a dir1/Dir3Video.nfo",

            "Mixed in a dir2/Video in a dir1/Video in a dir1/MixedVideo.mkv",
            "Mixed in a dir2/Video in a dir1/Video in a dir1/MixedVideo.nfo",
            "Mixed in a dir2/No video in a dir1/Video in a dir1/NoVideo1.nfo",
            "Mixed in a dir2/No video in a dir1/Video in a dir1/NoVideo2.nfo",
        };

        foreach (var file in files)
        {
            var filePath = Path.Join(libPath, file);
            new FileInfo(filePath).Directory?.Create();
            File.WriteAllText(filePath, string.Empty);
        }

        // firstLibrary.Videos.Add(new Video($"Fake video {suffix}", $"fake_video_{suffix}.mkv") { Duration = TimeSpan.FromMinutes(90) });
        await SaveChangesAsync();

        // AddWatchedVideo(firstLibrary.Videos[0], uno, 93);
        // AddWatchedVideo(firstLibrary.Videos[1], uno, 33);
        // AddWatchedVideo(firstLibrary.Videos[1], dos, 53);
        // AddWatchedVideo(firstLibrary.Videos[2], dos, 94);

        // await SaveChangesAsync();

        // void AddWatchedVideo(Video video, Watcher watcher, int percentage)
        // {
        //     WatcherVideos.Add(new()
        //     {
        //         VideoId = video.VideoId,
        //         WatcherId = watcher.WatcherId,
        //         Percentage = percentage,
        //         LastPosition = percentage / 100d * video.Duration!.Value
        //     });
        // }
    }
}