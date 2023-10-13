using System.ComponentModel;
using FFMpegCore;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using selflix.Db;

namespace selflix.Jobs;

public class IndexLibrary
{
    readonly AppDbContext _db;
    readonly ILogger<IndexLibrary> _logger;
    bool _isFullIndex;
    string _libPath = string.Empty;
    public IndexLibrary(AppDbContext db, ILogger<IndexLibrary> logger)
    {
        _db = db;
        _logger = logger;
    }
    [DisplayName("Index Library")]
    [AutomaticRetry(Attempts = 0)]

    // TODO: full index, incremental + duration for missing
    public async Task RunAsync(int libraryId, bool full, CancellationToken token)
    {
        _isFullIndex = full;
        var library = await _db.Libraries.Include(l => l.Videos).SingleOrDefaultAsync(l => l.LibraryId == libraryId, token);
        if (library == null)
        {
            _logger.LogError("Could not find library {LibraryId}", libraryId);
            return;
        }
        _libPath = C.Paths.MediaDataFor(library.MediaPath);
        if (!Directory.Exists(_libPath))
        {
            _logger.LogError("Library path {LibraryPath} does not exist or is currently unavailable", _libPath);
            if (library.LastFullIndexStarted > library.LastFullIndexCompleted)
            {
                library.LastFullIndexCompleted = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            return;
        }

        _logger.LogInformation("Indexing library {LibraryName} ({LibraryPath})", library.Name, _libPath);
        library.LastFullIndexStarted = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await ScanAndUpdateVideoFilesAsync(library, token);

        await RebuildDirHierarchy(libraryId, token);

        await AddMissingDirsToVideos(libraryId, token);

        library.LastFullIndexCompleted = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Indexing library {LibraryName} complete", library.Name);
    }

    async Task ScanAndUpdateVideoFilesAsync(Library library, CancellationToken token)
    {
        var existingVideoPaths = new HashSet<string>();
        // Remove missing
        _logger.LogDebug("Removing missing videos");
        foreach (var video in library.Videos)
        {
            var videoPath = Path.Join(_libPath, video.LibraryPath);
            if (!File.Exists(videoPath))
            {
                _db.Videos.Remove(video);
                _logger.LogDebug("Removed video {VideoTitle} ({VideoPath})", video.Title, videoPath);
            }
            else
                existingVideoPaths.Add(videoPath);
        }
        await _db.SaveChangesAsync(token);

        // Add new
        _logger.LogDebug("Adding new videos");
        var allFiles = Directory.EnumerateFiles(_libPath, "*", SearchOption.AllDirectories);
        foreach (var filePath in allFiles)
        {
            if (existingVideoPaths.Contains(filePath) || !C.IsVideoFile(filePath))
                continue;

            var video = await GetVideoAsync(filePath, token);
            library.Videos.Add(video);
            _logger.LogDebug("Added video {VideoTitle} ({VideoPath})", video.Title, filePath);
            await _db.SaveChangesAsync(token);
        }
        library.LastFullIndexCompleted = DateTime.UtcNow;
        await _db.SaveChangesAsync(token);
    }

    async Task<Video> GetVideoAsync(string filePath, CancellationToken token)
    {
        var title = Path.GetFileNameWithoutExtension(filePath);
        var relativePath = Path.GetRelativePath(_libPath, filePath);
        var video = new Video(title, relativePath);

        if (_isFullIndex)
            try
            {
                var mediaInfo = await FFProbe.AnalyseAsync(filePath, null, token);
                video.Duration = mediaInfo.Duration;
            }
            catch (Exception)
            {
                _logger.LogError("Cannot get media info for {FilePath}", filePath);
            }

        return video;
    }

    async Task RebuildDirHierarchy(int libraryId, CancellationToken token)
    {
        _logger.LogInformation("Rebuilding dir hierarchy");
        var levels = new Dictionary<int, Dictionary<string, string>>();
        var videoPaths = await _db.Videos.AsNoTracking().Where(v => v.LibraryId == libraryId).Select(v => v.LibraryPath).ToListAsync(token);
        foreach (var videoPath in videoPaths)
        {
            var parentDir = Path.GetDirectoryName(videoPath);
            if (string.IsNullOrWhiteSpace(parentDir))
                continue;
            var parentParts = parentDir.Split(Path.DirectorySeparatorChar);
            for (int i = 0; i < parentParts.Length; i++)
            {
                if (!levels.ContainsKey(i))
                    levels.Add(i, new());

                var dirName = parentParts[i];
                var dirPath = string.Join(Path.DirectorySeparatorChar, parentParts.Take(i + 1));
                if (!levels[i].ContainsKey(dirPath))
                    levels[i].Add(dirPath, dirName);
            }
        }

        foreach (var level in levels)
        {
            var existingDirs = await _db.Dirs.Where(d => d.LibraryId == libraryId && d.Depth == level.Key).ToDictionaryAsync(d => d.LibraryPath, d => d, token);
            // Remove no longer existing dirs
            foreach (var existingDir in existingDirs)
                if (!level.Value.ContainsKey(existingDir.Key))
                {
                    _db.Dirs.Remove(existingDir.Value);
                    _logger.LogDebug("Removing dir {DirPath}", existingDir.Key);
                }

            // Add new dirs
            var parentDirs = await _db.Dirs.Where(d => d.LibraryId == libraryId && d.Depth == level.Key - 1).ToDictionaryAsync(d => d.LibraryPath, d => d, token);
            foreach (var levelDir in level.Value)
                if (!existingDirs.ContainsKey(levelDir.Key))
                {
                    var newDir = new Dir(levelDir.Value, levelDir.Key)
                    {
                        Depth = level.Key,
                        LibraryId = libraryId,
                    };
                    var parentPath = Path.GetDirectoryName(levelDir.Key);
                    if (!string.IsNullOrWhiteSpace(parentPath) && parentDirs.TryGetValue(parentPath, out var parentDir))
                    {
                        parentDir.SubDirs.Add(newDir);
                        _logger.LogDebug("Adding dir {DirName} to parent {ParentDirName}", newDir.Name, parentDir.Name);
                    }
                    else
                    {
                        _db.Dirs.Add(newDir);
                        _logger.LogDebug("Adding dir {DirName} to root", newDir.Name);
                    }
                }
            await _db.SaveChangesAsync(token);
        }
    }

    async Task AddMissingDirsToVideos(int libraryId, CancellationToken token)
    {
        var dirs = await _db.Dirs.AsNoTracking().Where(d => d.LibraryId == libraryId).ToDictionaryAsync(d => d.LibraryPath, d => d.DirId, token);
        var videos = await _db.Videos.Where(v => v.LibraryId == libraryId && !v.DirId.HasValue).ToDictionaryAsync(v => v.LibraryPath, v => v, token);
        foreach (var video in videos)
        {
            var dirPath = Path.GetDirectoryName(video.Key);
            if (string.IsNullOrWhiteSpace(dirPath))
                continue;

            if (!dirs.TryGetValue(dirPath, out var dirId))
            {
                _logger.LogError("Could not find dir {DirPath} for movie", dirPath);
                continue;
            }
            else
            {
                _logger.LogDebug("Found dir for video {VideoTitle}", video.Value.Title);
                video.Value.DirId = dirId;
            }
        }
        await _db.SaveChangesAsync(token);
    }
}