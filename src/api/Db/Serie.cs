namespace selflix.Db;

public class Serie
{
    Serie()
    {
        Title = null!;
        LibraryPath = null!;
    }
    internal Serie(string title, string libraryPath)
    {
        Title = title;
        LibraryPath = libraryPath;
    }
    public int SerieId { get; set; }
    public int LibraryId { get; set; }
    public string Title { get; set; }
    public string LibraryPath { get; set; }
    public int Year { get; set; }

    public Library? Library { get; set; }
    public virtual List<SerieEpisode> Episodes { get; set; } = new();
}