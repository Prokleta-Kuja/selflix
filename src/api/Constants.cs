using System.Globalization;

namespace selflix;

public static class C
{
    public static readonly bool IsDebug;
    public static readonly TimeZoneInfo TZ;
    public static readonly CultureInfo Locale;
    public static readonly string PostgresConnectionString;
    public static readonly DbContextType DbContextType;
    static C()
    {
        IsDebug = Environment.GetEnvironmentVariable("DEBUG") == "1";
        PostgresConnectionString = Environment.GetEnvironmentVariable("POSTGRES") ?? string.Empty;
        DbContextType = !string.IsNullOrWhiteSpace(PostgresConnectionString) ? DbContextType.PostgreSQL : DbContextType.SQLite;

        try
        {
            TZ = TimeZoneInfo.FindSystemTimeZoneById(Environment.GetEnvironmentVariable("TZ") ?? "America/Chicago");
        }
        catch (Exception)
        {
            TZ = TimeZoneInfo.Local;
        }

        try
        {
            Locale = CultureInfo.GetCultureInfo(Environment.GetEnvironmentVariable("LOCALE") ?? "en-US");
        }
        catch (Exception)
        {
            Locale = CultureInfo.InvariantCulture;
        }
    }
    static readonly HashSet<string> s_videoExtensions = new(StringComparer.InvariantCultureIgnoreCase) { ".mkv", ".avi", ".mp4", ".mpeg" };
    public static bool IsVideoFile(string path)
    {
        var ext = Path.GetExtension(path);
        return s_videoExtensions.Contains(ext);
    }
    public static class Paths
    {
        static string Root => IsDebug ? Path.Join(Environment.CurrentDirectory, "/data") : "/data";
        public static readonly string ConfigData = $"{Root}/config";
        public static string ConfigDataFor(string file) => Path.Combine(ConfigData, file);
        public static readonly string MediaData = $"{Root}/media";
        public static string MediaDataFor(string file) => Path.Combine(MediaData, file);
        public static readonly string Sqlite = ConfigDataFor("app.db");
        public static readonly string Hangfire = ConfigDataFor("jobs.db");
        public static readonly string AppDbConnectionString = $"Data Source={Sqlite}";
        public static readonly string HangfireConnectionString = $"Data Source={Hangfire}";
    }
}
public enum DbContextType
{
    SQLite,
    PostgreSQL,
}