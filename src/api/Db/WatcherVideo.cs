namespace selflix.Db;

public class WatcherVideo
{
    public int WatcherId { get; set; }
    public int VideoId { get; set; }
    public TimeSpan LastPosition { get; set; }
    public int Percentage { get; set; }

    public Watcher? Watcher { get; set; }
    public Video? Video { get; set; }
}