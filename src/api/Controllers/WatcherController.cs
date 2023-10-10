using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using selflix.Db;
using selflix.Models;

namespace selflix.Controllers;

[ApiController]
[Route("api/watchers")]
[Tags(nameof(Watcher))]
[Produces("application/json")]
[ProducesErrorResponseType(typeof(PlainError))]
public class WatcherController : AppControllerBase
{
    readonly ILogger<WatcherController> _logger;
    readonly AppDbContext _db;
    readonly IDataProtectionProvider _dpProvider;
    public WatcherController(ILogger<WatcherController> logger, AppDbContext db, IDataProtectionProvider dpProvider)
    {
        _logger = logger;
        _db = db;
        _dpProvider = dpProvider;
    }

    [HttpGet(Name = "GetAllWatchers")]
    [ProducesResponseType(typeof(ListResponse<WatcherLM>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync([FromQuery] WatcherQuery req)
    {
        var query = _db.Watchers.AsNoTracking();

        if (!TryGetAuthToken(out var token))
            return BadRequest(new PlainError("Could not determine user"));

        if (req.UserId.HasValue && token.IsAdmin)
            query = query.Where(ud => ud.UserId == req.UserId.Value);
        else
            query = query.Where(ud => ud.UserId == token.UserId);

        if (!string.IsNullOrWhiteSpace(req.SearchTerm))
            query = query.Where(ud => EF.Functions.Like(ud.Name, $"%{req.SearchTerm}%"));

        var count = await query.CountAsync();

        if (!string.IsNullOrWhiteSpace(req.SortBy) && Enum.TryParse<WatcherSortBy>(req.SortBy, true, out var sortBy))
            query = sortBy switch
            {
                WatcherSortBy.Name => query.Order(ud => ud.Name, req.Ascending),
                _ => query
            };

        var items = await query
            .Paginate(req)
            .Select(ud => new WatcherLM
            {
                Id = ud.WatcherId,
                Name = ud.Name,
            })
            .ToListAsync();

        return Ok(new ListResponse<WatcherLM>(req, count, items));
    }


    [HttpPost(Name = "CreateWatcher")]
    [ProducesResponseType(typeof(WatcherVM), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync(WatcherCM model)
    {
        model.Name = model.Name.Trim();

        if (model.IsInvalid(out var errorModel))
            return BadRequest(errorModel);

        if (!TryGetAuthToken(out var token))
            return BadRequest(new PlainError("Could not determine user"));

        var watcher = new Watcher(model.Name)
        {
            UserId = token.UserId,
        };

        _db.Watchers.Add(watcher);
        await _db.SaveChangesAsync();

        return Ok(new WatcherVM(watcher));
    }

    [HttpPut("{WatcherId}", Name = "UpdateWatcher")]
    [ProducesResponseType(typeof(WatcherVM), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(int WatcherId, WatcherUM model)
    {
        model.Name = model.Name.Trim();

        if (model.IsInvalid(out var errorModel))
            return BadRequest(errorModel);

        if (!TryGetAuthToken(out var token))
            return BadRequest(new PlainError("Could not determine user"));

        var watcher = await _db.Watchers.SingleOrDefaultAsync(ud => ud.UserId == token.UserId && ud.WatcherId == WatcherId);
        if (watcher == null)
            return NotFound(new PlainError("Not found"));

        watcher.Name = model.Name;
        await _db.SaveChangesAsync();

        return Ok(new WatcherVM(watcher));
    }

    [HttpDelete("{WatcherId}", Name = "DeleteWatcher")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(int WatcherId)
    {
        if (!TryGetAuthToken(out var token))
            return BadRequest(new PlainError("Could not determine user"));

        var query = _db.Watchers.AsQueryable();
        if (User.FindFirstValue(ClaimTypes.Role) != C.ADMIN_ROLE)
            query = query.Where(ud => ud.UserId == token.UserId);

        var Watcher = await query.SingleOrDefaultAsync(ud => ud.WatcherId == WatcherId);
        if (Watcher == null)
            return NotFound(new PlainError("Not found"));

        _db.Watchers.Remove(Watcher);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public class WatcherQuery : FilterQuery
{
    public int? UserId { get; set; }
}

public enum WatcherSortBy
{
    Name = 0,
}