using Microsoft.AspNetCore.Identity;

namespace CleanCom.Infrastructure.Identity;

/// <summary>
/// ASP.NET Identity user. Roles: Administracion, Chofer, Invitado.
/// </summary>
public class User : IdentityUser<int>
{
    public string FullName { get; set; } = "";
    public bool Active { get; set; } = true;
    /// <summary>The initial or temporary password must be changed on the next sign-in.</summary>
    public bool MustChangePassword { get; set; } = true;
    public DateTimeOffset? LastAccess { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class Role : IdentityRole<int>
{
    public Role() { }
    public Role(string name) : base(name) { }
}

/// <summary>
/// Refresh token. Only its hash is stored; every use replaces it with a new one.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
