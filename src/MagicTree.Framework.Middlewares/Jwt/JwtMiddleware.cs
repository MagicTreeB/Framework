using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace MagicTree.Framework.Middlewares.Jwt;

/// <summary>
/// Middleware to extract and make JWT claims easily accessible
/// </summary>
public class JwtMiddleware
{
    private readonly RequestDelegate _next;

    public JwtMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract JWT info if user is authenticated
        if (context.User.Identity?.IsAuthenticated ?? false)
        {
            var jwtInfo = ExtractJwtInfo(context.User);
            if (jwtInfo != null)
            {
                // Store JWT info in HttpContext.Items for easy access
                context.Items["JwtInfo"] = jwtInfo;
            }
        }

        await _next(context);
    }

    private JwtInfo? ExtractJwtInfo(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        var organizationIdClaim = user.FindFirst("organization_id")?.Value
            ?? user.FindFirst("OrganizationId")?.Value;
        
        return new JwtInfo
        {
            UserId = userId,
            Username = user.FindFirst(ClaimTypes.Name)?.Value
                ?? user.FindFirst("name")?.Value,
            Email = user.FindFirst(ClaimTypes.Email)?.Value
                ?? user.FindFirst("email")?.Value,
            OrganizationId = !string.IsNullOrEmpty(organizationIdClaim) && Guid.TryParse(organizationIdClaim, out var orgId)
                ? orgId
                : null,
            Roles = user.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList(),
            Permissions = user.FindAll("permission")
                .Select(c => c.Value)
                .Distinct()
                .ToList(),
            Claims = user.Claims
                .GroupBy(c => c.Type)
                .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(c => c.Value)))
        };
    }
}