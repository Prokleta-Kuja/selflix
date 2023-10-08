namespace selflix.Db;

public class AuthToken
{
    public int AuthTokenId { get; set; }
    public int UserId { get; set; }
    public AuthApp App { get; set; }
    public DateTime Issued { get; set; }
    public DateTime Expires { get; set; }

    public User? User { get; set; }
}

public enum AuthApp
{
    Unknown = 0,
    Web = 1,
    Android = 2,
    Player = 3,
}