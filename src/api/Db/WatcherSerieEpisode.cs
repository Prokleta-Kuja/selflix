namespace selflix.Db;

public class WatcherSerieEpisode
{
    public int WatcherId { get; set; }
    public int SerieEpisodeId { get; set; }
    public TimeSpan LastPosition { get; set; }
    public int Percentage { get; set; }

    public Watcher? Watcher { get; set; }
    public SerieEpisode? SerieEpisode { get; set; }
}