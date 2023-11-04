namespace selflix.Db;

public class User
{
    User()
    {
        Name = null!;
        NameNormalized = null!;
        TimeZone = null!;
        Locale = null!;
        PasswordHash = null!;
    }
    internal User(string name, string passwordHash, bool isAdmin = false)
    {
        Name = name;
        NameNormalized = name.ToLowerInvariant();
        TimeZone = C.TZ.Id;
        Locale = C.Locale.Name;
        PasswordHash = passwordHash;
        IsAdmin = isAdmin;
        Created = DateTime.UtcNow;
    }

    public int UserId { get; set; }
    public string Name { get; set; }
    public string NameNormalized { get; set; }
    public string TimeZone { get; set; }
    public string Locale { get; set; }
    public string PasswordHash { get; set; }
    public bool IsAdmin { get; set; }
    public byte[]? OtpKey { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Disabled { get; set; }
    public DateTime? LastLogin { get; set; }

    public virtual List<AuthToken> AuthTokens { get; set; } = new();
    public virtual List<StreamToken> StreamTokens { get; set; } = new();
    public virtual List<Library> Libraries { get; set; } = new();
    public virtual List<UserDevice> Devices { get; set; } = new();
    public virtual List<Watcher> Watchers { get; set; } = new();
}