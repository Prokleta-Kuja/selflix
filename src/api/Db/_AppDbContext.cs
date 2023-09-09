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
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Serie> Series => Set<Serie>();
    public DbSet<SerieEpisode> SerieEpisodes => Set<SerieEpisode>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();
    public DbSet<Watcher> Watchers => Set<Watcher>();
    public DbSet<WatcherVideo> WatcherVideos => Set<WatcherVideo>();
    public DbSet<WatcherMovie> WatcherMovies => Set<WatcherMovie>();
    public DbSet<WatcherSerieEpisode> WatcherSerieEpisodes => Set<WatcherSerieEpisode>();

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
            e.HasMany(e => e.Movies).WithOne(e => e.Library).HasForeignKey(e => e.LibraryId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(e => e.Series).WithOne(e => e.Library).HasForeignKey(e => e.LibraryId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Video>(e =>
        {
            e.HasKey(e => e.VideoId);
            e.HasMany(e => e.Views).WithOne(e => e.Video).HasForeignKey(e => e.VideoId).OnDelete(DeleteBehavior.Cascade);
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

        builder.Entity<WatcherVideo>(e =>
        {
            e.HasKey(e => new { e.WatcherId, e.VideoId });
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

        var otherLibrary = new Library("Fake other", "fake_other", LibraryType.Unknown);
        var movieLibrary = new Library("Fake movies", "fake_movie", LibraryType.Movies);
        var serieLibrary = new Library("Fake series", "fake_series", LibraryType.Series);

        Libraries.AddRange(otherLibrary, movieLibrary, serieLibrary);
        await SaveChangesAsync();

        devUser.Libraries.Add(otherLibrary); devUser.Libraries.Add(movieLibrary); devUser.Libraries.Add(serieLibrary);
        usrUser.Libraries.Add(otherLibrary); usrUser.Libraries.Add(movieLibrary); usrUser.Libraries.Add(serieLibrary);

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

        var suffixes = new List<string> { "ABC", "DEF", "GHI" };
        foreach (var suffix in suffixes)
        {
            otherLibrary.Videos.Add(new Video($"Fake video {suffix}", $"fake_video_{suffix}.mkv") { Duration = TimeSpan.FromMinutes(90) });
            movieLibrary.Movies.Add(new Movie($"Fake movie {suffix}", $"fake_movie_{suffix}.mkv") { Duration = TimeSpan.FromMinutes(120), Year = 2021 });
            var series = new Serie($"Fake series {suffix}", $"fake_series_{suffix}");
            for (int s = 1; s <= 2; s++)
                for (int e = 1; e <= 5; e++)
                {
                    var id = $"S{s.ToString().PadLeft(2, '0')}E{e.ToString().PadLeft(2, '0')}";
                    series.Episodes.Add(new SerieEpisode($"Episode {id}", $"fake_episode_{id}.mkv")
                    {
                        Duration = TimeSpan.FromMinutes(45),
                        Season = s,
                        Episode = e,
                    });
                }
            serieLibrary.Series.Add(series);
        }
        await SaveChangesAsync();

        AddWatchedVideo(otherLibrary.Videos[0], uno, 93);
        AddWatchedVideo(otherLibrary.Videos[1], uno, 33);
        AddWatchedVideo(otherLibrary.Videos[1], dos, 53);
        AddWatchedVideo(otherLibrary.Videos[2], dos, 94);

        AddWatchedMovie(movieLibrary.Movies[0], uno, 97);
        AddWatchedMovie(movieLibrary.Movies[1], uno, 44);
        AddWatchedMovie(movieLibrary.Movies[1], dos, 68);
        AddWatchedMovie(movieLibrary.Movies[2], dos, 95);

        AddWatchedEpisode(serieLibrary.Series[0], uno, 1, 1, 5);
        AddWatchedEpisode(serieLibrary.Series[1], uno, 1, 1, 3);
        AddWatchedEpisode(serieLibrary.Series[1], dos, 1, 1, 5);
        AddWatchedEpisode(serieLibrary.Series[2], dos, 1, 1, 4);

        await SaveChangesAsync();

        void AddWatchedVideo(Video video, Watcher watcher, int percentage)
        {
            WatcherVideos.Add(new()
            {
                VideoId = video.VideoId,
                WatcherId = watcher.WatcherId,
                Percentage = percentage,
                LastPosition = percentage / 100d * video.Duration!.Value
            });
        }

        void AddWatchedMovie(Movie movie, Watcher watcher, int percentage)
        {
            WatcherMovies.Add(new()
            {
                MovieId = movie.MovieId,
                WatcherId = watcher.WatcherId,
                Percentage = percentage,
                LastPosition = percentage / 100d * movie.Duration!.Value
            });
        }

        void AddWatchedEpisode(Serie serie, Watcher watcher, int season, int firstEpisode, int lastEpisode)
        {
            var episodes = serie.Episodes.Where(e => e.Season == season && e.Episode >= firstEpisode && e.Episode <= lastEpisode);
            foreach (var episode in episodes)
                WatcherSerieEpisodes.Add(new()
                {
                    SerieEpisodeId = episode.SerieEpisodeId,
                    WatcherId = watcher.WatcherId,
                    Percentage = 92,
                    LastPosition = 92 / 100d * episode.Duration!.Value
                });
        }
    }
}