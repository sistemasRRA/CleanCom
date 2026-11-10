namespace CleanCom.Domain.Time;

/// <summary>
/// Business clock. Every cutoff and business date is evaluated in Mexico City time;
/// the database stores UTC. It is injected so cutoffs can be tested.
/// </summary>
public interface IBusinessClock
{
    /// <summary>Current instant in UTC (what goes into the database).</summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>Current instant with the Mexico City offset.</summary>
    DateTimeOffset LocalNow { get; }

    /// <summary>Today's business date in Mexico City.</summary>
    DateOnly Today { get; }
}
