using System;

namespace MMO.Core.Middlewares.Jwt;

/// <summary>
/// JWT information extracted from the token
/// </summary>
public class JwtInfo
{
    public string UserId { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Email { get; set; }
    public Guid? OrganizationId { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public Dictionary<string, string> Claims { get; set; } = new();
}
