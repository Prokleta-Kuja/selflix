using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using selflix.Auth;

namespace selflix.Controllers;

public class AppControllerBase : ControllerBase
{
    protected bool TryGetAuthToken(out CacheAuthToken token)
    {
        token = null!;
        var tokenVal = User.FindFirstValue(ClaimTypes.SerialNumber);
        if (string.IsNullOrWhiteSpace(tokenVal) || !int.TryParse(tokenVal, out var tokenId))
            return false;

        var cache = HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
        var key = AppAuthenticationHandler.AuthenticationCacheKey(tokenId);
        return cache.TryGetValue(key, out token!);
    }
}