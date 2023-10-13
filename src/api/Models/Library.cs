using System.ComponentModel.DataAnnotations;
using selflix.Db;

namespace selflix.Models;

public class LibraryVM
{
    public LibraryVM(Library l)
    {
        Id = l.LibraryId;
        Name = l.Name;
        MediaPath = l.MediaPath;
        LastFullIndexStarted = l.LastFullIndexStarted;
        LastFullIndexCompleted = l.LastFullIndexCompleted;
    }

    [Required] public int Id { get; set; }
    [Required] public string Name { get; set; }
    [Required] public string MediaPath { get; set; }
    public DateTime? LastFullIndexStarted { get; set; }
    public DateTime? LastFullIndexCompleted { get; set; }
}

public class LibraryLM
{
    [Required] public int Id { get; set; }
    [Required] public required string Name { get; set; }
    [Required] public required string MediaPath { get; set; }
    public required bool Indexing { get; set; }
    public DateTime? LastFullIndexCompleted { get; set; }
}

public class LibraryCM
{
    [Required] public required string Name { get; set; }
    [Required] public required string MediaPath { get; set; }
    public bool IsInvalid(IEnumerable<string> exitingPaths, out ValidationError errorModel)
    {
        errorModel = new();

        if (string.IsNullOrWhiteSpace(Name))
            errorModel.Errors.Add(nameof(Name), "Required");

        if (string.IsNullOrWhiteSpace(MediaPath))
            errorModel.Errors.Add(nameof(MediaPath), "Required");
        else if (exitingPaths.Contains(MediaPath))
            errorModel.Errors.Add(nameof(MediaPath), "Path already added");
        else if (!Path.Exists(C.Paths.MediaDataFor(MediaPath)))
            errorModel.Errors.Add(nameof(MediaPath), "Path does not exist");

        return errorModel.Errors.Count > 0;
    }
}

public class LibraryUM
{
    [Required] public required string Name { get; set; }
    public bool IsInvalid(out ValidationError errorModel)
    {
        errorModel = new();

        if (string.IsNullOrWhiteSpace(Name))
            errorModel.Errors.Add(nameof(Name), "Required");

        return errorModel.Errors.Count > 0;
    }
}