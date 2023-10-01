using System.ComponentModel;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using selflix.Db;

namespace selflix.Jobs;

public class IndexLibrary
{
    readonly AppDbContext _db;
    readonly IBackgroundJobClient _job;
    readonly ILogger<IndexLibrary> _logger;
    public IndexLibrary(AppDbContext db, IBackgroundJobClient job, ILogger<IndexLibrary> logger)
    {
        _db = db;
        _job = job;
        _logger = logger;
    }
    [DisplayName("Index Library")]
    [AutomaticRetry(Attempts = 0)]
    public async Task RunAsync(int libraryId, CancellationToken token)
    {
        var library = await _db.Libraries.Include(l => l.Videos).SingleOrDefaultAsync(l => l.LibraryId == libraryId, token);
        if (library == null)
        {
            _logger.LogError("Could not find library {LibraryId}", libraryId);
            return;
        }
        var libPath = C.Paths.MediaDataFor(library.MediaPath);
        if (!Directory.Exists(libPath))
        {
            _logger.LogError("Library path {LibraryPath} does not exist", libPath);
            return;
        }

        _logger.LogInformation("Indexing library {LibraryName} ({LibraryPath})", library.Name, libPath);

        var existingVideoPaths = new HashSet<string>();
        // Remove missing
        _logger.LogInformation("Removing missing videos");
        foreach (var video in library.Videos)
        {
            var videoPath = Path.Join(libPath, video.LibraryPath);
            if (!File.Exists(videoPath))
            {
                _db.Videos.Remove(video);
                _logger.LogInformation("Removed video {VideoTitle} ({VideoPath})", video.Title, videoPath);
            }
            else
                existingVideoPaths.Add(videoPath);
        }
        await _db.SaveChangesAsync(token);

        // Add new
        _logger.LogInformation("Adding new videos");
        var allFiles = Directory.EnumerateFiles(libPath, "*", SearchOption.AllDirectories);
        foreach (var filePath in allFiles)
        {
            if (existingVideoPaths.Contains(filePath) || !C.IsVideoFile(filePath))
                continue;

            var video = await GetVideoAsync(libPath, filePath, token);
            library.Videos.Add(video);
            _logger.LogInformation("Added video {VideoTitle} ({VideoPath})", video.Title, filePath);
        }
        await _db.SaveChangesAsync(token);

        _logger.LogInformation("Indexing library {LibraryName} complete", library.Name);
    }

    async Task<Video> GetVideoAsync(string libPath, string filePath, CancellationToken token)
    {
        var title = Path.GetFileNameWithoutExtension(filePath);
        var relativePath = Path.GetRelativePath(libPath, filePath);
        var video = new Video(title, relativePath);

        // TODO: get duration from nfo or ffmpeg. ffmpeg could also know about subs, but maybe another job
        await Task.CompletedTask;
        return video;
    }
}