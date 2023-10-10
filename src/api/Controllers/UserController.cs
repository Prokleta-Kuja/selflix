using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using selflix.Db;
using selflix.Models;
using selflix.Services;

namespace selflix.Controllers;

[ApiController]
[Route("api/users")]
[Tags(nameof(Db.User))]
[Produces("application/json")]
[ProducesErrorResponseType(typeof(PlainError))]
public class UsersController : AppControllerBase
{
    readonly ILogger<UsersController> _logger;
    readonly AppDbContext _db;
    readonly HibpService _hibp;
    readonly IPasswordHasher _hasher;

    public UsersController(ILogger<UsersController> logger, AppDbContext db, HibpService hibp, IPasswordHasher hasher)
    {
        _logger = logger;
        _db = db;
        _hibp = hibp;
        _hasher = hasher;
    }

    [HttpGet(Name = "GetUsers")]
    [ProducesResponseType(typeof(ListResponse<UserLM>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync([FromQuery] UserQuery req)
    {
        if (!TryGetAuthToken(out var token) || !token.IsAdmin)
            return Forbid();

        var query = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(req.SearchTerm))
            query = query.Where(u => u.Name.Contains(req.SearchTerm.ToLower()));

        var count = await query.CountAsync();

        if (!string.IsNullOrWhiteSpace(req.SortBy) && Enum.TryParse<UsersSortBy>(req.SortBy, true, out var sortBy))
            query = sortBy switch
            {
                UsersSortBy.Name => query.Order(u => u.Name, req.Ascending),
                UsersSortBy.IsAdmin => query.Order(u => u.IsAdmin, req.Ascending),
                UsersSortBy.Disabled => query.Order(u => u.Disabled, req.Ascending),
                _ => query
            };

        var items = await query
            .Paginate(req)
            .Select(u => new UserLM
            {
                Id = u.UserId,
                Name = u.Name,
                IsAdmin = u.IsAdmin,
                Disabled = u.Disabled,
            })
            .ToListAsync();

        return Ok(new ListResponse<UserLM>(req, count, items));
    }

    [HttpGet("{userId}", Name = "GetUser")]
    [ProducesResponseType(typeof(UserVM), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOneAsnyc(int userId)
    {
        if (!TryGetAuthToken(out var token) || !token.IsAdmin)
            return Forbid();

        var user = await _db.Users
           .AsNoTracking()
           .Where(u => u.UserId == userId)
           .Select(u => new UserVM(u))
           .FirstOrDefaultAsync();

        if (user == null)
            return NotFound(new PlainError("Not found"));

        return Ok(user);
    }

    [HttpPost(Name = "CreateUser")]
    [ProducesResponseType(typeof(UserVM), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync(UserCM model)
    {
        if (!TryGetAuthToken(out var token) || !token.IsAdmin)
            return Forbid();

        model.Name = model.Name.Trim().ToLower();

        if (model.IsInvalid(out var errorModel))
            return BadRequest(errorModel);

        var isDuplicate = await _db.Users
            .AsNoTracking()
            .Where(u => u.Name == model.Name)
            .AnyAsync();

        if (isDuplicate)
            return BadRequest(new ValidationError(nameof(model.Name), "Already exists"));

        var hibpResult = await _hibp.CheckAsync(model.Password);
        if (!string.IsNullOrWhiteSpace(hibpResult))
            return BadRequest(new ValidationError(nameof(model.Password), hibpResult));

        // TODO: validate complexity
        var hash = _hasher.HashPassword(model.Password);
        var user = new User(model.Name, hash, model.IsAdmin);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new UserVM(user));
    }

    [HttpPut("{userId}", Name = "UpdateUser")]
    [ProducesResponseType(typeof(UserVM), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(int userId, UserUM model)
    {
        if (!TryGetAuthToken(out var token) || !token.IsAdmin)
            return Forbid();

        var user = await _db.Users
          .Where(u => u.UserId == userId)
          .FirstOrDefaultAsync();

        if (user == null)
            return NotFound(new PlainError("Not found"));

        model.Name = model.Name.Trim().ToLower();

        if (model.IsInvalid(out var errorModel))
            return BadRequest(errorModel);

        var isDuplicate = await _db.Users
            .AsNoTracking()
            .Where(u => u.UserId != user.UserId && u.Name == model.Name)
            .AnyAsync();

        if (isDuplicate)
            return BadRequest(new ValidationError(nameof(model.Name), "Already exists"));

        user.Name = model.Name;
        user.IsAdmin = model.IsAdmin;
        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            // TODO: validate complexity
            var hibpResult = await _hibp.CheckAsync(model.NewPassword);
            if (!string.IsNullOrWhiteSpace(hibpResult))
                return BadRequest(new ValidationError(nameof(model.NewPassword), hibpResult));

            user.PasswordHash = _hasher.HashPassword(model.NewPassword);
        }
        if (model.Disabled.HasValue)
            user.Disabled = model.Disabled.Value ? user.Disabled.HasValue ? user.Disabled : DateTime.UtcNow : null;
        if (model.ClearOtpKey.HasValue && model.ClearOtpKey.Value)
            user.OtpKey = null;

        await _db.SaveChangesAsync();

        return Ok(new UserVM(user));
    }

    [HttpPatch("{userId}/actions/disable", Name = "ToggleDisabled")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleDisableAsync(int userId)
    {
        if (!TryGetAuthToken(out var token) || !token.IsAdmin)
            return Forbid();

        var user = await _db.Users
          .Where(u => u.UserId == userId)
          .FirstOrDefaultAsync();

        if (user == null)
            return NotFound(new PlainError("Not found"));

        user.Disabled = user.Disabled.HasValue ? null : DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{userId}", Name = "DeleteUser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(int userId)
    {
        if (!TryGetAuthToken(out var token) || !token.IsAdmin)
            return Forbid();

        var user = await _db.Users
          .Where(u => u.UserId == userId)
          .FirstOrDefaultAsync();

        if (user == null)
            return NotFound(new PlainError("Not found"));

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public class UserQuery : FilterQuery { }

public enum UsersSortBy
{
    Name = 0,
    IsAdmin = 1,
    Disabled = 2,
}