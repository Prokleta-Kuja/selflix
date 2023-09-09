namespace selflix.Db;

public class SerieEpisode
{
    SerieEpisode()
    {
        Title = null!;
        SeriePath = null!;
    }
    internal SerieEpisode(string title, string seriePath)
    {
        Title = title;
        SeriePath = seriePath;
    }
    public int SerieEpisodeId { get; set; }
    public int SerieId { get; set; }
    public string Title { get; set; }
    public string SeriePath { get; set; }
    public int? Season { get; set; }
    public int? Episode { get; set; }
    public TimeSpan? Duration { get; set; }

    public Serie? Serie { get; set; }
    public virtual List<WatcherSerieEpisode> Views { get; set; } = new();
}