using System.ComponentModel;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using selflix.Db;

namespace selflix.Jobs;

public class FullIndex
{
    readonly ILogger<FullIndex> _logger;
    readonly AppDbContext _db;
    public FullIndex(ILogger<FullIndex> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }
    [DisplayName("Full index")]
    [AutomaticRetry(Attempts = 0)]
    public async Task RunAsync(int libraryId, CancellationToken token)
    {
        _logger.LogInformation("Full index starting");

        var library = await _db.Libraries.SingleOrDefaultAsync(l => l.LibraryId == libraryId, token);
        if (library == null)
        {
            _logger.LogError("Could not find library {LibraryId}", libraryId);
            return;
        }

        var root = C.Paths.MediaDataFor(library.MediaPath);
        if (!Directory.Exists(root))
        {
            _logger.LogError("Library path {LibraryPath} does not exist", root);
            return;
        }

        await IndexDirectoryAsync(root, token);
    }

    public async Task IndexDirectoryAsync(string dirPath, CancellationToken token)
    {
        _logger.LogDebug("Indexing directory {DirectoryPath}", dirPath);
        foreach (var path in Directory.GetDirectories(dirPath))
            await IndexDirectoryAsync(path, token);

        var files = Directory.GetFiles(dirPath);
        var videos = files.Where(C.IsVideoFile);
    }
}