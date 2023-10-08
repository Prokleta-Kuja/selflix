using System.ComponentModel;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using selflix.Db;

namespace selflix.Jobs;

public class ClearAuthTokens
{
    readonly AppDbContext _db;
    readonly ILogger<IndexAll> _logger;
    public ClearAuthTokens(AppDbContext db, ILogger<IndexAll> logger)
    {
        _db = db;
        _logger = logger;
    }

    [DisplayName("Clear expired auth tokens")]
    [AutomaticRetry(Attempts = 0)]
    public async Task RunAsync(CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var expiredTokensDeleted = await _db.AuthTokens.Where(at => at.Expires <= now).ExecuteDeleteAsync(token);
        if (expiredTokensDeleted > 0)
            _logger.LogInformation("Deleted {ExpiredCount} expired tokens", expiredTokensDeleted);
    }
}