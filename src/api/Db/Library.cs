namespace selflix.Db;

public class Library
{
    Library()
    {
        Name = null!;
        MediaPath = null!;
    }
    internal Library(string name, string mediaPath, LibraryType type)
    {
        Name = name;
        MediaPath = mediaPath;
        Type = type;
    }
    public int LibraryId { get; set; }
    public required string Name { get; set; }
    public required string MediaPath { get; set; }
    public DateTime? LastIndex { get; set; }
    public LibraryType Type { get; set; }

    public virtual List<User> Users { get; set; } = new();
    public virtual List<Movie> Movies { get; set; } = new();
    public virtual List<Serie> Series { get; set; } = new();
}

public enum LibraryType
{
    Unknown = 0,
    Movies = 1,
    Series = 2,
}