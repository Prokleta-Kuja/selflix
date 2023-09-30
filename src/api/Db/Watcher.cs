namespace selflix.Db;

public class Watcher
{
    Watcher()
    {
        Name = null!;
    }
    internal Watcher(string name)
    {
        Name = name;
    }

    public int WatcherId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; }

    public User? User { get; set; }
    public virtual List<WatcherVideo> Videos { get; set; } = new();
}