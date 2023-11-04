namespace selflix.Db;

public class Video
{
    Video()
    {
        Title = null!;
        LibraryPath = null!;
    }
    internal Video(string title, string libraryPath)
    {
        Title = title;
        LibraryPath = libraryPath;
    }
    public int VideoId { get; set; }
    public int LibraryId { get; set; }
    public int? DirId { get; set; }
    public string Title { get; set; }
    public string LibraryPath { get; set; }
    public TimeSpan? Duration { get; set; }

    public Library? Library { get; set; }
    public Dir? Dir { get; set; }
    public virtual List<WatcherVideo> Views { get; set; } = new();
    public virtual List<StreamToken> StreamTokens { get; set; } = new();
}