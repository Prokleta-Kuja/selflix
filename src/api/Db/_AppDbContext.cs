using System.Diagnostics;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using selflix.Services;

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
        });

        builder.Entity<User>(e =>
        {
            e.HasKey(e => e.UserId);
        });

        builder.Entity<UserDevice>(e =>
        {
            e.HasKey(e => e.UserDeviceId);
            e.HasOne(e => e.User).WithMany(e => e.Devices).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
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
                    builder.Entity(entityType.Name).Property(property.Name).HasConversion<double>();
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
            return;


        await SaveChangesAsync();
    }
}