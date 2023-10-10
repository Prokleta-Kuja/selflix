using System.ComponentModel.DataAnnotations;
using selflix.Db;

namespace selflix.Models;

public class WatcherVM
{
    public WatcherVM(Watcher w)
    {
        Id = w.WatcherId;
        Name = w.Name;
    }

    [Required] public int Id { get; set; }
    [Required] public string Name { get; set; }
}

public class WatcherLM
{
    [Required] public int Id { get; set; }
    [Required] public required string Name { get; set; }
}

public class WatcherCM
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

public class WatcherUM
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