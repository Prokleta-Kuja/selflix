namespace selflix.Db;

public class WatcherMovie
{
    public int WatcherId { get; set; }
    public int MovieId { get; set; }
    public TimeSpan LastPosition { get; set; }
    public int Percentage { get; set; }

    public Watcher? Watcher { get; set; }
    public Movie? Movie { get; set; }
}