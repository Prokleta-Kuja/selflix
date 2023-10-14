using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using selflix.Db;
using selflix.Models;

namespace selflix.Controllers;

[ApiController]
[Route("api/libraries")]
[Tags(nameof(Library))]
[Produces("application/json")]
[ProducesErrorResponseType(typeof(PlainError))]
public class LibraryController : AppControllerBase
{
    readonly ILogger<LibraryController> _logger;
    readonly AppDbContext _db;
    readonly IBackgroundJobClient _job;
    public LibraryController(ILogger<LibraryController> logger, AppDbContext db, IBackgroundJobClient job)
    {
        _logger = logger;
        _db = db;
        _job = job;
    }

    [HttpGet(Name = "GetAllLibraries")]
    [ProducesResponseType(typeof(ListResponse<LibraryLM>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync([FromQuery] LibraryQuery req)
    {
        var query = _db.Libraries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(req.SearchTerm))
            query = query.Where(ud => EF.Functions.Like(ud.Name, $"%{req.SearchTerm}%"));

        var count = await query.CountAsync();

        if (!string.IsNullOrWhiteSpace(req.SortBy) && Enum.TryParse<LibrarySortBy>(req.SortBy, true, out var sortBy))
            query = sortBy switch
            {
                LibrarySortBy.Name => query.Order(ud => ud.Name, req.Ascending),
                _ => query
            };

        var items = await query
            .Paginate(req)
            .Select(l => new LibraryLM
            {
                Id = l.LibraryId,
                Name = l.Name,
                MediaPath = l.MediaPath,
                Indexing = l.LastFullIndexStarted > l.LastFullIndexCompleted,
                LastFullIndexCompleted = l.LastFullIndexCompleted,
            })
            .ToListAsync();

        return Ok(new ListResponse<LibraryLM>(req, count, items));
    }

    [HttpPost(Name = "CreateLibrary")]
    [ProducesResponseType(typeof(LibraryVM), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync(LibraryCM model)
    {
        model.Name = model.Name.Trim();

        if (!TryGetAuthToken(out var token) || !token.IsAdmin)
            return Forbid();

        var existingPaths = await _db.Libraries.AsNoTracking().Select(l => l.MediaPath).ToListAsync();

        if (model.IsInvalid(existingPaths, out var errorModel))
            return BadRequest(errorModel);

        var library = new Library(model.Name, model.MediaPath);

        _db.Libraries.Add(library);
        await _db.SaveChangesAsync();

        return Ok(new LibraryVM(library));
    }

    [HttpPut("{LibraryId}", Name = "UpdateLibrary")]
    [ProducesResponseType(typeof(LibraryVM), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(int libraryId, LibraryUM model)
    {
        model.Name = model.Name.Trim();

        if (model.IsInvalid(out var errorModel))
            return BadRequest(errorModel);

        if (!TryGetAuthToken(out var token) || !token.IsAdmin)
            return Forbid();

        var library = await _db.Libraries.SingleOrDefaultAsync(l => l.LibraryId == libraryId);
        if (library == null)
            return NotFound(new PlainError("Not found"));

        library.Name = model.Name;
        await _db.SaveChangesAsync();

        return Ok(new LibraryVM(library));
    }

    [HttpDelete("{libraryId}", Name = "DeleteLibrary")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(int libraryId)
    {
        if (!TryGetAuthToken(out var token) || !token.IsAdmin)
            return Forbid();

        var library = await _db.Libraries.SingleOrDefaultAsync(l => l.LibraryId == libraryId);
        if (library == null)
            return NotFound(new PlainError("Not found"));

        _db.Libraries.Remove(library);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{libraryId}/actions/schedule-index/{fullIndex}", Name = "ScheduleIndex")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ScheduleFullIndexAsync(int libraryId, bool fullIndex)
    {
        if (!TryGetAuthToken(out var token) || !token.IsAdmin)
            return Forbid();

        var library = await _db.Libraries.SingleOrDefaultAsync(l => l.LibraryId == libraryId);
        if (library == null)
            return NotFound(new PlainError("Not found"));

        library.LastFullIndexStarted = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        _job.Enqueue<Jobs.IndexLibrary>(j => j.RunAsync(libraryId, fullIndex, CancellationToken.None));

        return NoContent();
    }

    [HttpGet("{libraryId}/dirs/{directoryId:int?}", Name = "GetDirectory")]
    [ProducesResponseType(typeof(DirVM), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDirectoryAsync(int libraryId, int? directoryId)
    {
        var query = _db.Dirs
            .Include(d => d.SubDirs)
            .Include(d => d.Videos)
            .AsQueryable();

        if (directoryId.HasValue)
            query = query.Where(d => d.DirId == directoryId.Value && d.LibraryId == libraryId);
        else
            query = query.Where(d => d.LibraryId == libraryId && !d.ParentDirId.HasValue);

        var dir = await query.SingleOrDefaultAsync();
        if (dir == null)
            return NotFound(new PlainError("Not found"));

        var model = new DirVM(dir);
        return Ok(model);
    }
}
public class LibraryQuery : FilterQuery
{
}

public enum LibrarySortBy
{
    Name = 0,
    MediaPath = 1,
    LastIndex = 2,
}