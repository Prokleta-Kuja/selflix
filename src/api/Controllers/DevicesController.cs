using System.Security.Claims;
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
public class DevicesController : ControllerBase
{
    readonly ILogger<DevicesController> _logger;
    readonly AppDbContext _db;
    readonly IDataProtectionProvider _dpProvider;
    public DevicesController(ILogger<DevicesController> logger, AppDbContext db, IDataProtectionProvider dpProvider)
    {
        _logger = logger;
        _db = db;
        _dpProvider = dpProvider;
    }

    [HttpGet(Name = "GetAllMyDevices")]
    [ProducesResponseType(typeof(ListResponse<UserDeviceLM>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync([FromQuery] UserDeviceQuery req)
    {
        var query = _db.UserDevices.AsNoTracking();

        var sidClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Sid)?.Value;
        if (sidClaim == null || !int.TryParse(sidClaim, out var userId))
            return BadRequest(new PlainError("Could not determine userid"));

        query = query.Where(ud => ud.UserId == userId);

        if (!string.IsNullOrWhiteSpace(req.SearchTerm))
            query = query.Where(ud => EF.Functions.Like(ud.Name, $"%{req.SearchTerm}%")
                || EF.Functions.Like(ud.Brand!, $"%{req.SearchTerm}%")
                || EF.Functions.Like(ud.Model!, $"%{req.SearchTerm}%")
                || EF.Functions.Like(ud.OS!, $"%{req.SearchTerm}%"));

        var count = await query.CountAsync();

        if (!string.IsNullOrWhiteSpace(req.SortBy) && Enum.TryParse<UserDevicesSortBy>(req.SortBy, true, out var sortBy))
            query = sortBy switch
            {
                UserDevicesSortBy.Name => query.Order(ud => ud.Name, req.Ascending),
                UserDevicesSortBy.Brand => query.Order(ud => ud.Brand, req.Ascending),
                UserDevicesSortBy.Model => query.Order(ud => ud.Model, req.Ascending),
                UserDevicesSortBy.OS => query.Order(ud => ud.OS, req.Ascending),
                UserDevicesSortBy.Created => query.Order(ud => ud.Created, req.Ascending),
                UserDevicesSortBy.LastLogin => query.Order(ud => ud.LastLogin, req.Ascending),
                _ => query
            };

        var items = await query
            .Paginate(req)
            .Select(ud => new UserDeviceLM
            {
                UserDeviceId = ud.UserDeviceId,
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
        model.Name = model.Name.Trim();
        model.DeviceId = model.DeviceId.ToUpper();

        if (model.IsInvalid(out var errorModel))
            return BadRequest(errorModel);

        var sidClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Sid)?.Value;
        if (sidClaim == null || !int.TryParse(sidClaim, out var userId))
            return BadRequest(new PlainError("Could not determine userid"));

        var userDevice = new UserDevice(userId, model.DeviceId, model.Name)
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
        model.Name = model.Name.Trim();

        if (model.IsInvalid(out var errorModel))
            return BadRequest(errorModel);

        var sidClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Sid)?.Value;
        if (sidClaim == null || !int.TryParse(sidClaim, out var userId))
            return BadRequest(new PlainError("Could not determine userid"));

        var userDevice = await _db.UserDevices.SingleOrDefaultAsync(ud => ud.UserId == userId && ud.UserDeviceId == userDeviceId);
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
        var sidClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Sid)?.Value;
        if (sidClaim == null || !int.TryParse(sidClaim, out var userId))
            return BadRequest(new PlainError("Could not determine userid"));

        var userDevice = await _db.UserDevices.SingleOrDefaultAsync(ud => ud.UserId == userId && ud.UserDeviceId == userDeviceId);
        if (userDevice == null)
            return NotFound(new PlainError("Not found"));

        _db.UserDevices.Remove(userDevice);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{deviceId}/actions/register", Name = "RegisterDevice")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegisterUserDeviceAsync(string deviceId)
    {
        deviceId = deviceId.ToUpper();
        var userDevice = await _db.UserDevices.SingleOrDefaultAsync(ud => ud.DeviceId == deviceId);
        if (userDevice == null)
            return NotFound(new PlainError("Not found"));

        if (userDevice.OtpKey != null)
            return BadRequest(new PlainError("Device was previously registered, clear existing registration to reregister"));

        var token = TotpService.CreateAuthToken(nameof(selflix), userDevice.DeviceId, nameof(selflix));
        var secret = Base32.FromBase32(token.Secret);
        var protector = _dpProvider.CreateProtector(nameof(userDevice.OtpKey));
        userDevice.OtpKey = protector.Protect(secret);
        await _db.SaveChangesAsync();

        return Ok(token.Secret);
    }
}

public class UserDeviceQuery : FilterQuery { }

public enum UserDevicesSortBy
{
    Name = 0,
    Brand = 1,
    Model = 2,
    OS = 3,
    Created = 4,
    LastLogin = 5,
}