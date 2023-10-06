using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using selflix.Models;
using selflix.Services;
using selflix.Db;

namespace selflix.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/auth")]
[Tags("Auth")]
[Produces("application/json")]
[ProducesErrorResponseType(typeof(PlainError))]
public class AuthController : ControllerBase
{
    const string OTP_CLAIM = "OTP";
    const string GENERIC_ERROR_MESSAGE = "Invalid username, password and/or one time code";
    const string GENERIC_DEVICE_ERROR_MESSAGE = "Invalid DeviceId and/or one time code";
    readonly ILogger<AuthController> _logger;
    readonly AppDbContext _db;
    readonly IDataProtectionProvider _dpProvider;
    readonly IPasswordHasher _hasher;

    public AuthController(ILogger<AuthController> logger, AppDbContext db, IDataProtectionProvider dpProvider, IPasswordHasher hasher)
    {
        _logger = logger;
        _db = db;
        _dpProvider = dpProvider;
        _hasher = hasher;
    }

    [HttpGet(Name = "Status")]
    [ProducesResponseType(typeof(AuthStatusModel), StatusCodes.Status200OK)]
    public IActionResult Status()
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            var hasOtp = User.HasClaim(c => c.Type == OTP_CLAIM);
            var expires = DateTime.MinValue;
            var expiresStr = User.FindFirst(ClaimTypes.Expiration)?.Value;
            if (!string.IsNullOrWhiteSpace(expiresStr) && long.TryParse(expiresStr, out var expiresVal))
                expires = DateTime.FromBinary(expiresVal);

            return Ok(new AuthStatusModel
            {
                Authenticated = true,
                HasOtp = hasOtp,
                Username = User.Identity!.Name,
                Expires = expires
            });
        }
        else
            return Ok(new AuthStatusModel { Authenticated = false, HasOtp = false });
    }

    [Authorize]
    [HttpGet("totp", Name = "GetTotp")]
    [ProducesResponseType(typeof(TotpVM), StatusCodes.Status200OK)]
    public IActionResult GetTotp()
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated || string.IsNullOrWhiteSpace(User.Identity.Name))
            return BadRequest(new PlainError("Not authenticated or no username"));

        var token = TotpService.CreateAuthToken(nameof(selflix), User.Identity.Name, nameof(selflix));
        var chunks = token.Secret.Chunk(4).Select(c => new string(c));
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(token.Uri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(5);

        var result = new TotpVM
        {
            ChunkedSecret = string.Join(' ', chunks),
            Qr = $"data:image/png;base64,{Convert.ToBase64String(qrCodeImage)}",
        };

        return Ok(result);
    }

    [Authorize]
    [HttpPost("totp", Name = "SaveTotp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveTotp(TotpCM model)
    {
        var secretArr = model.ChunkedSecret.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var secret = string.Join(string.Empty, secretArr);
        var key = Base32.FromBase32(secret);

        if (model.IsInvalid(key, out var errorModel))
            return BadRequest(errorModel);

        if (User.Identity == null || !User.Identity.IsAuthenticated || string.IsNullOrWhiteSpace(User.Identity.Name))
            return BadRequest(new PlainError("Not authenticated or no username"));

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Name == User.Identity.Name.ToLower());
        if (user == null)
            return BadRequest(new PlainError("User not found"));

        var protector = _dpProvider.CreateProtector(nameof(user.OtpKey));
        user.OtpKey = protector.Protect(key);

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPatch(Name = "AutoLogin")]
    [ProducesResponseType(typeof(AuthStatusModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AutoLoginAsync()
    {
        var hasAdmins = await _db.Users.AsNoTracking().AnyAsync(u => u.IsAdmin);
        if (hasAdmins)
            return BadRequest(new PlainError("There are admin users in database, autologin disabled."));

        _logger.LogInformation("No admin users in database, autologing in...");
        var expires = DateTime.UtcNow.AddMinutes(10);
        var claims = new List<Claim> {
            new(ClaimTypes.Name, "temporary admin"),
            new(ClaimTypes.Role, C.ADMIN_ROLE),
            new(ClaimTypes.Expiration, expires.ToBinary().ToString()),
            new(OTP_CLAIM,"1"),
        };
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties { AllowRefresh = false, ExpiresUtc = expires, IsPersistent = true };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

        return Ok(new AuthStatusModel { Authenticated = true, HasOtp = true, Username = "temporary admin", Expires = expires });
    }

    [HttpPost(Name = "Login")]
    [ProducesResponseType(typeof(AuthStatusModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LoginAsync(LoginModel model)
    {
        model.Username = model.Username.ToLower();
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Name == model.Username && u.Disabled == null);

        if (user == null)
            return BadRequest(new PlainError(GENERIC_ERROR_MESSAGE));

        var passwordResult = _hasher.VerifyHashedPassword(user.PasswordHash, model.Password);
        if (passwordResult == PasswordVerificationResult.Failed)
            return BadRequest(new PlainError(GENERIC_ERROR_MESSAGE));
        else if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = _hasher.HashPassword(model.Password);

        var hasOtp = user.OtpKey != null;
        if (hasOtp)
        {
            var protector = _dpProvider.CreateProtector(nameof(user.OtpKey));
            var key = protector.Unprotect(user.OtpKey!);
            if (!model.Totp.HasValue || !TotpService.ValidateCode(key, model.Totp.Value))
                return BadRequest(new PlainError(GENERIC_ERROR_MESSAGE));
        }

        user.LastLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var expires = DateTime.UtcNow.AddHours(1);
        var claims = new List<Claim> {
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Sid, user.UserId.ToString()),
            new(ClaimTypes.Expiration, expires.ToBinary().ToString())
        };
        if (user.IsAdmin)
            claims.Add(new Claim(ClaimTypes.Role, C.ADMIN_ROLE));
        if (hasOtp)
            claims.Add(new Claim(OTP_CLAIM, "1"));

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties { AllowRefresh = false, ExpiresUtc = expires, IsPersistent = true };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

        return Ok(new AuthStatusModel { Authenticated = true, HasOtp = hasOtp, Username = user.Name, IsAdmin = user.IsAdmin, Expires = expires });
    }

    [HttpDelete(Name = "Logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LogoutAsync()
    {
        if (HttpContext.User.Identity?.IsAuthenticated ?? false)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpPost(Name = "DeviceLogin")]
    [ProducesResponseType(typeof(AuthStatusModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeviceLoginAsync(DeviceLoginModel model)
    {
        model.DeviceId = model.DeviceId.ToUpper();
        var userDevice = await _db.UserDevices
        .Include(ud => ud.User)
        .SingleOrDefaultAsync(ud => ud.DeviceId == model.DeviceId);

        if (userDevice == null || userDevice.OtpKey == null)
            return BadRequest(new PlainError(GENERIC_DEVICE_ERROR_MESSAGE));

        var protector = _dpProvider.CreateProtector(nameof(userDevice.OtpKey));
        var key = protector.Unprotect(userDevice.OtpKey!);
        if (model.IsInvalid(key, out var errorModel))
            return BadRequest(errorModel);

        userDevice.LastLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var expires = DateTime.UtcNow.AddHours(3);
        var claims = new List<Claim> {
            // TODO: add claim to differentiate user vs device
            new(ClaimTypes.Name, userDevice.User!.Name),
            new(ClaimTypes.Sid, userDevice.UserDeviceId.ToString()),
            new(ClaimTypes.Expiration, expires.ToBinary().ToString())
        };

        //TODO: figure out, cookie or jwt?
        // var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        // var authProperties = new AuthenticationProperties { AllowRefresh = false, ExpiresUtc = expires, IsPersistent = true };
        // await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

        return Ok(new AuthStatusModel { Authenticated = true, HasOtp = true, Username = userDevice.User.Name, IsAdmin = userDevice.User.IsAdmin, Expires = expires });
    }
}