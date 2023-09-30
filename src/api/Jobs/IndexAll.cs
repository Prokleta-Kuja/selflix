using System.ComponentModel;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using selflix.Db;

namespace selflix.Jobs;

public class IndexAll
{
    readonly AppDbContext _db;
    readonly IBackgroundJobClient _job;
    readonly ILogger<IndexAll> _logger;
    public IndexAll(AppDbContext db, IBackgroundJobClient job, ILogger<IndexAll> logger)
    {
        _db = db;
        _job = job;
        _logger = logger;
    }
    [DisplayName("Index All")]
    [AutomaticRetry(Attempts = 0)]
    public async Task RunAsync(CancellationToken token)
    {
        _logger.LogInformation("Indexing all libraries");

        var libraries = await _db.Libraries.ToListAsync(token);
        foreach (var library in libraries)
        {
            _job.Enqueue<IndexLibrary>(j => j.RunAsync(library.LibraryId, CancellationToken.None));
        }

        _logger.LogDebug("Scheduled {LibraryCount} library index jobs", libraries.Count);
    }
}