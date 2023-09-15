namespace selflix.Db;

public class Library
{
    Library()
    {
        Name = null!;
        MediaPath = null!;
    }
    internal Library(string name, string mediaPath)
    {
        Name = name;
        MediaPath = mediaPath;
    }
    public int LibraryId { get; set; }
    public string Name { get; set; }
    public string MediaPath { get; set; }
    public DateTime? LastIndex { get; set; }

    public virtual List<User> Users { get; set; } = new();
    public virtual List<Video> Videos { get; set; } = new();
}