using Microsoft.AspNetCore.Identity;

namespace CleanCom.Infrastructure.Identity;

/// <summary>
/// ASP.NET Identity user. System roles are in <see cref="CleanCom.Domain.Access.Roles"/>; the office can add more.
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

/// <summary>
/// Name (inherited) is the key stored in the token, for example "Logistica";
/// DisplayName is what the user sees, "Logística".
/// </summary>
public class Role : IdentityRole<int>
{
    public Role() { }
    public Role(string name) : base(name) { }

    public string DisplayName { get; set; } = "";
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
