using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using selflix.Db;

namespace selflix.Auth;

public class AppAuthenticationHandler : SignInAuthenticationHandler<AppAuthenticationOptions>
{
    public const string AUTHENTICATION_SCHEME = "App";
    public const string AUTHENTICATION_COOKIE = "auth.app";
    public const string OTP_CLAIM = "OTP";

    public AppAuthenticationHandler(IOptionsMonitor<AppAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
    {
    }

    public static string AuthenticationCacheKey(int id) => $"auth.{id}";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var encryptedTokenId = string.Empty;
        if (Request.Headers.TryGetValue(HeaderNames.Authorization, out var headerVal))
        {
            var header = headerVal.ToString();
            if (header.StartsWith(AUTHENTICATION_SCHEME, StringComparison.InvariantCultureIgnoreCase))
                encryptedTokenId = header.Replace(AUTHENTICATION_SCHEME, string.Empty).Trim();
        }
        else if (Request.Cookies.TryGetValue(AUTHENTICATION_COOKIE, out var cookieVal))
            encryptedTokenId = cookieVal;
        else
            return AuthenticateResult.NoResult();

        if (string.IsNullOrWhiteSpace(encryptedTokenId))
            return AuthenticateResult.Fail("Invalid token");

        var dp = Context.RequestServices.GetDataProtector(AUTHENTICATION_SCHEME);
        var tokenIdVal = dp.Unprotect(encryptedTokenId);
        if (!int.TryParse(tokenIdVal, out var tokenId))
            return AuthenticateResult.Fail("Invalid token");

        var cache = Context.RequestServices.GetRequiredService<IMemoryCache>();
        var cachedToken = await cache.GetOrCreateAsync(AuthenticationCacheKey(tokenId), async entry =>
        {
            var now = DateTime.UtcNow;
            var db = Context.RequestServices.GetRequiredService<AppDbContext>();
            var token = await db.AuthTokens
                .AsNoTracking()
                .Include(at => at.User)
                .SingleOrDefaultAsync(at => at.AuthTokenId == tokenId && at.Expires > now && !at.User!.Disabled.HasValue);

            if (token == null)
                return null;

            return new CacheAuthToken(token);
        });

        if (cachedToken == null)
            return AuthenticateResult.Fail("Invalid/expired token");

        var claimsIdentity = cachedToken.ToClaimsIdentity();
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(claimsIdentity), AUTHENTICATION_SCHEME);

        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties? properties)
    {
        var tokenId = user.FindFirstValue(ClaimTypes.SerialNumber);
        if (string.IsNullOrWhiteSpace(tokenId))
            throw new ArgumentNullException(nameof(tokenId));

        var expiresStr = user.FindFirstValue(ClaimTypes.Expiration);
        if (string.IsNullOrWhiteSpace(expiresStr) || !long.TryParse(expiresStr, out var expiresVal))
            throw new ArgumentNullException(nameof(expiresStr));

        var dp = Context.RequestServices.GetDataProtector(AUTHENTICATION_SCHEME);
        var encryptedToken = dp.Protect(tokenId);
        var expires = DateTime.FromBinary(expiresVal);
        var opt = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            IsEssential = true,
            SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
            Expires = expires,
        };
        Response.Cookies.Append(AUTHENTICATION_COOKIE, encryptedToken, opt);
        return Task.CompletedTask;
    }

    protected override Task HandleSignOutAsync(AuthenticationProperties? properties)
    {
        Response.Cookies.Delete(AUTHENTICATION_COOKIE);
        return Task.CompletedTask;
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return base.HandleChallengeAsync(properties);
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return base.HandleForbiddenAsync(properties);
    }
}

public class CacheAuthToken
{
    CacheAuthToken()
    {
        UserName = null!;
        TimeZone = null!;
        Locale = null!;
    }
    internal CacheAuthToken(AuthToken token)
    {
        AuthTokenId = token.AuthTokenId;
        UserId = token.UserId;
        Issued = token.Issued;
        Expires = token.Expires;
        App = token.App;

        if (token.User == null)
            UserName = TimeZone = Locale = string.Empty;
        else
        {
            HasOtp = token.User.OtpKey != null;
            UserName = token.User.Name;
            TimeZone = token.User.TimeZone;
            Locale = token.User.Locale;
            IsAdmin = token.User.IsAdmin;
        }
    }
    public int AuthTokenId { get; set; }
    public int UserId { get; set; }
    public AuthApp App { get; set; }
    public DateTime Issued { get; set; }
    public DateTime Expires { get; set; }

    public bool HasOtp { get; set; }
    public string UserName { get; set; }
    public string TimeZone { get; set; }
    public string Locale { get; set; }
    public bool IsAdmin { get; set; }
    public ClaimsIdentity ToClaimsIdentity()
    {
        var claims = new List<Claim>{
            new (ClaimTypes.SerialNumber, AuthTokenId.ToString()),
            new (ClaimTypes.Sid, UserId.ToString()),
            new (ClaimTypes.Spn, App.ToString()),
            new (ClaimTypes.AuthenticationInstant, Issued.ToBinary().ToString()),
            new (ClaimTypes.Expiration, Expires.ToBinary().ToString()),
        };
        if (!string.IsNullOrWhiteSpace(UserName))
            claims.Add(new(ClaimTypes.Name, UserName));
        if (IsAdmin)
            claims.Add(new(ClaimTypes.Role, C.ADMIN_ROLE));
        if (HasOtp)
            claims.Add(new(AppAuthenticationHandler.OTP_CLAIM, "1"));

        var identity = new ClaimsIdentity(claims, AppAuthenticationHandler.AUTHENTICATION_SCHEME);
        return identity;
    }

}

public class AppAuthenticationOptions : AuthenticationSchemeOptions
{

}
public static class AppAuthenticationExtensions
{
    public static AuthenticationBuilder AddAppAuthentication(this AuthenticationBuilder builder)
        => builder.AddAppAuthentication(AppAuthenticationHandler.AUTHENTICATION_SCHEME);
    public static AuthenticationBuilder AddAppAuthentication(this AuthenticationBuilder builder, string authenticationScheme)
        => builder.AddAppAuthentication(authenticationScheme, configureOptions: null!);
    public static AuthenticationBuilder AddAppAuthentication(this AuthenticationBuilder builder, Action<AppAuthenticationOptions> configureOptions)
        => builder.AddAppAuthentication(AppAuthenticationHandler.AUTHENTICATION_SCHEME, configureOptions);
    public static AuthenticationBuilder AddAppAuthentication(this AuthenticationBuilder builder, string authenticationScheme, Action<AppAuthenticationOptions> configureOptions)
        => builder.AddAppAuthentication(authenticationScheme, displayName: null, configureOptions: configureOptions);
    public static AuthenticationBuilder AddAppAuthentication(this AuthenticationBuilder builder, string authenticationScheme, string? displayName, Action<AppAuthenticationOptions> configureOptions)
        => builder.AddScheme<AppAuthenticationOptions, AppAuthenticationHandler>(authenticationScheme, displayName, configureOptions);
}