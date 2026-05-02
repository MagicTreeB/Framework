using Microsoft.AspNetCore.Http;
using MagicTree.Framework.Middlewares.Jwt;
using MagicTree.Framework.Services.JwtService;
using MagicTree.Framework.Share;

namespace MagicTree.Framework.Services.Jwt;

/// <summary>
/// JWT service implementation for accessing current user information
/// </summary>
public class JwtService : IJwtService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public JwtService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    public JwtInfo? GetJwtInfo()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.GetJwtInfo();
    }

    /// <inheritdoc/>
    public string GetUserId()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.GetUserId() ??  ShareKey.SystemUser;
    }

    /// <inheritdoc/>
    public string GetUserName()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.GetUsername() ??  ShareKey.SystemUser;
    }

    /// <inheritdoc/>
    public Guid GetOrganizationId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return Guid.Empty;

        var jwtInfo = context.GetJwtInfo();
        if (jwtInfo?.Claims.TryGetValue("organization_id", out var orgIdString) == true)
        {
            if (Guid.TryParse(orgIdString, out var orgId))
            {
                return orgId;
            }
        }

        return Guid.Empty;
    }
}
