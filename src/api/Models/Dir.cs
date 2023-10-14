using System.ComponentModel.DataAnnotations;
using selflix.Db;

namespace selflix.Models;

public class DirVM
{
    public DirVM(Dir d)
    {
        Id = d.DirId;
        Name = d.Name;
        ParentDirId = d.ParentDirId;

        SubDirs = d.SubDirs.Select(s => new SubDirVM(s)).ToArray();
        Videos = d.Videos.Select(v => new VideoVM(v)).ToArray();
    }

    [Required] public int Id { get; set; }
    [Required] public string Name { get; set; }
    public int? ParentDirId { get; set; }
    public SubDirVM[] SubDirs { get; set; }
    public VideoVM[] Videos { get; set; }
}

public class SubDirVM
{
    public SubDirVM(Dir d)
    {
        Id = d.DirId;
        Name = d.Name;
    }

    [Required] public int Id { get; set; }
    [Required] public string Name { get; set; }
}

public class VideoVM
{
    public VideoVM(Video v)
    {
        Id = v.VideoId;
        Title = v.Title;
        Duration = v.Duration.HasValue ? Convert.ToInt64(v.Duration.Value.TotalMilliseconds) : null;
    }
    [Required] public int Id { get; set; }
    [Required] public string Title { get; set; }
    public long? Duration { get; set; }
}