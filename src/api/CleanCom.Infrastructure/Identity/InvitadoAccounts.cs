using CleanCom.Application.Common;
using CleanCom.Domain.Time;
using CleanCom.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Infrastructure.Identity;

/// <summary>The invitados' users: read their state and block or unblock their access from the CRM.</summary>
public class InvitadoAccounts(CleanComDbContext db, IBusinessClock clock) : IInvitadoAccounts
{
    public async Task<IReadOnlyDictionary<int, AppUserInfo>> InfoAsync(IReadOnlyCollection<int> userIds, CancellationToken ct) =>
        userIds.Count == 0
            ? new Dictionary<int, AppUserInfo>()
            : await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => new AppUserInfo(u.Active, u.LastAccess), ct);

    public async Task SetActiveAsync(int userId, bool active, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("La cuenta de la app no existe.");
        user.Active = active;
        if (!active)
        {
            // The app cannot renew its session; the access token it holds ends in minutes.
            var now = clock.UtcNow;
            await db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
        }
        await db.SaveChangesAsync(ct);
    }
}
