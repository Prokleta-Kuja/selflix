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

        foreach (var sub in d.SubDirs)
            SubDirs.Add(new SubDirVM(sub));
        foreach (var vid in d.Videos)
            Videos.Add(new VideoVM(vid));
    }

    [Required] public int Id { get; set; }
    [Required] public string Name { get; set; }
    public int? ParentDirId { get; set; }
    public List<SubDirVM> SubDirs { get; set; } = new();
    public List<VideoVM> Videos { get; set; } = new();
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
        Duration = v.Duration;
    }
    [Required] public int Id { get; set; }
    [Required] public string Title { get; set; }
    public TimeSpan? Duration { get; set; }
}