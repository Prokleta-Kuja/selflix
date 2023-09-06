namespace selflix.Db;

public class Library
{
    public int LibraryId { get; set; }
    public required string Name { get; set; }

    public virtual List<User> Users { get; set; } = new();
}

public enum LibraryType
{
    Unknown = 0,
    Movies = 1,
    Series = 2,
}