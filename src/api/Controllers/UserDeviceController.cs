using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using selflix.Db;
using selflix.Models;
using selflix.Services;

namespace selflix.Controllers;

[ApiController]
[Route("api/devices")]
[Tags(nameof(UserDevice))]
[Produces("application/json")]
[ProducesErrorResponseType(typeof(PlainError))]
public class UserDeviceController : AppControllerBase
{
    const string CHARS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    static readonly byte[] s_chars = CHARS.Select(Convert.ToByte).ToArray();

    readonly ILogger<UserDeviceController> _logger;
    readonly AppDbContext _db;
    readonly IDataProtectionProvider _dpProvider;
    public UserDeviceController(ILogger<UserDeviceController> logger, AppDbContext db, IDataProtectionProvider dpProvider)
    {
        _logger = logger;
        _db = db;
        _dpProvider = dpProvider;
    }

    [HttpGet(Name = "GetAllDevices")]
    [ProducesResponseType(typeof(ListResponse<UserDeviceLM>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync([FromQuery] UserDeviceQuery req)
    {
        if (!TryGetAuthToken(out var token))
            return BadRequest(new PlainError("Could not determine user"));

        var query = _db.UserDevices.AsNoTracking();

        if (req.UserId.HasValue && token.IsAdmin)
            query = query.Where(ud => ud.UserId == req.UserId.Value);
        else
            query = query.Where(ud => ud.UserId == token.UserId);

        if (!string.IsNullOrWhiteSpace(req.SearchTerm))
            query = query.Where(ud => EF.Functions.Like(ud.Name, $"%{req.SearchTerm}%")
                || EF.Functions.Like(ud.Brand!, $"%{req.SearchTerm}%")
                || EF.Functions.Like(ud.Model!, $"%{req.SearchTerm}%")
                || EF.Functions.Like(ud.OS!, $"%{req.SearchTerm}%"));

        var count = await query.CountAsync();

        if (!string.IsNullOrWhiteSpace(req.SortBy) && Enum.TryParse<UserDeviceSortBy>(req.SortBy, true, out var sortBy))
            query = sortBy switch
            {
                UserDeviceSortBy.Name => query.Order(ud => ud.Name, req.Ascending),
                UserDeviceSortBy.Brand => query.Order(ud => ud.Brand, req.Ascending),
                UserDeviceSortBy.Model => query.Order(ud => ud.Model, req.Ascending),
                UserDeviceSortBy.OS => query.Order(ud => ud.OS, req.Ascending),
                UserDeviceSortBy.Created => query.Order(ud => ud.Created, req.Ascending),
                UserDeviceSortBy.LastLogin => query.Order(ud => ud.LastLogin, req.Ascending),
                _ => query
            };

        var items = await query
            .Paginate(req)
            .Select(ud => new UserDeviceLM
            {
                Id = ud.UserDeviceId,
                Name = ud.Name,
                Brand = ud.Brand,
                Model = ud.Model,
                OS = ud.OS,
                Created = ud.Created,
                LastLogin = ud.LastLogin,
            })
            .ToListAsync();

        return Ok(new ListResponse<UserDeviceLM>(req, count, items));
    }


    [HttpPost(Name = "CreateUserDevice")]
    [ProducesResponseType(typeof(UserDeviceVM), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync(UserDeviceCM model)
    {
        if (!TryGetAuthToken(out var token))
            return BadRequest(new PlainError("Could not determine user"));

        model.Name = model.Name.Trim();
        var chunks = model.DeviceId.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        model.DeviceId = string.Join(string.Empty, chunks).ToUpper();

        if (model.IsInvalid(out var errorModel))
            return BadRequest(errorModel);

        // TODO: add devices as admin for this user
        var userDevice = new UserDevice(token.UserId, model.DeviceId, model.Name)
        {
            Created = DateTime.UtcNow,
        };

        _db.UserDevices.Add(userDevice);
        await _db.SaveChangesAsync();

        return Ok(new UserDeviceVM(userDevice));
    }

    [HttpPut("{userDeviceId}", Name = "UpdateUserDevice")]
    [ProducesResponseType(typeof(UserDeviceVM), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(int userDeviceId, UserDeviceUM model)
    {
        if (!TryGetAuthToken(out var token))
            return BadRequest(new PlainError("Could not determine user"));

        model.Name = model.Name.Trim();

        if (model.IsInvalid(out var errorModel))
            return BadRequest(errorModel);

        var userDevice = await _db.UserDevices.SingleOrDefaultAsync(ud => ud.UserId == token.UserId && ud.UserDeviceId == userDeviceId);
        if (userDevice == null)
            return NotFound(new PlainError("Not found"));

        userDevice.Name = model.Name;
        if (model.ClearOtpKey.HasValue && model.ClearOtpKey.Value)
            userDevice.OtpKey = null;
        await _db.SaveChangesAsync();

        return Ok(new UserDeviceVM(userDevice));
    }

    [HttpDelete("{userDeviceId}", Name = "DeleteUserDevice")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(int userDeviceId)
    {
        if (!TryGetAuthToken(out var token))
            return BadRequest(new PlainError("Could not determine user"));

        var query = _db.UserDevices.AsQueryable();
        if (User.FindFirstValue(ClaimTypes.Role) != C.ADMIN_ROLE)
            query = query.Where(ud => ud.UserId == token.UserId);

        var userDevice = await query.SingleOrDefaultAsync(ud => ud.UserDeviceId == userDeviceId);
        if (userDevice == null)
            return NotFound(new PlainError("Not found"));

        _db.UserDevices.Remove(userDevice);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [AllowAnonymous]
    [HttpPatch("actions/generate-device-id", Name = "GenerateDeviceId")]
    [ProducesResponseType(typeof(DeviceIdVM), StatusCodes.Status200OK)]
    public IActionResult GenerateDeviceIdAsync()
    {
        var deviceId = IPasswordHasher.GeneratePassword(9, s_chars);
        var chunks = deviceId.Chunk(3).Select(c => new string(c));

        var model = new DeviceIdVM
        {
            DeviceId = deviceId,
            DeviceIdChunked = string.Join(' ', chunks),
        };
        return Ok(model);
    }

    [AllowAnonymous]
    [HttpPatch("{deviceId}/actions/register", Name = "RegisterDevice")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegisterUserDeviceAsync(string deviceId)
    {
        var userDevice = await _db.UserDevices.SingleOrDefaultAsync(ud => ud.DeviceId == deviceId);
        if (userDevice == null)
            return NotFound(new PlainError("Not found"));

        if (userDevice.OtpKey != null)
            return BadRequest(new PlainError("Device was previously registered, clear existing registration to reregister"));

        var token = TotpService.CreateTotpToken(nameof(selflix), userDevice.Name, nameof(selflix));
        var secret = Base32.FromBase32(token.Secret);
        var protector = _dpProvider.CreateProtector(nameof(userDevice.OtpKey));
        userDevice.OtpKey = protector.Protect(secret);
        await _db.SaveChangesAsync();

        return Ok(token.Secret);
    }
}

public class UserDeviceQuery : FilterQuery
{
    public int? UserId { get; set; }
}

public enum UserDeviceSortBy
{
    Name = 0,
    Brand = 1,
    Model = 2,
    OS = 3,
    Created = 4,
    LastLogin = 5,
}