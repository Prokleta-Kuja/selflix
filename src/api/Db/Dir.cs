namespace selflix.Db;

public class Dir
{
    Dir()
    {
        Name = null!;
        LibraryPath = null!;
    }
    internal Dir(string name, string libraryPath)
    {
        Name = name;
        LibraryPath = libraryPath;
    }
    public int DirId { get; set; }
    public int LibraryId { get; set; }
    public int? ParentDirId { get; set; }
    public int Depth { get; set; }
    public string Name { get; set; }
    public string LibraryPath { get; set; }

    public Library? Library { get; set; }
    public Dir? ParentDir { get; set; }
    public virtual List<Dir> SubDirs { get; set; } = new();
    public virtual List<Video> Videos { get; set; } = new();
}