namespace CleanCom.Domain.Common;

/// <summary>
/// Base for every entity: identifier and audit fields. Dates are stored in UTC.
/// </summary>
public abstract class Entity
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public int? ModifiedBy { get; set; }
}

/// <summary>
/// Entity with soft delete: it is never removed, only marked as inactive.
/// </summary>
public interface IActivatable
{
    bool Active { get; set; }
}
