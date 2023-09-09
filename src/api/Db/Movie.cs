namespace selflix.Db;

public class Movie
{
    Movie()
    {
        Title = null!;
        LibraryPath = null!;
    }
    internal Movie(string title, string libraryPath)
    {
        Title = title;
        LibraryPath = libraryPath;
    }
    public int MovieId { get; set; }
    public int LibraryId { get; set; }
    public string Title { get; set; }
    public string LibraryPath { get; set; }
    public int Year { get; set; }
    public TimeSpan? Duration { get; set; }

    public Library? Library { get; set; }
    public virtual List<WatcherMovie> Views { get; set; } = new();
}