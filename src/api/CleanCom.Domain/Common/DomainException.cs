namespace CleanCom.Domain.Common;

/// <summary>
/// A business rule is not met. The message is shown to the user, so it is written in Spanish.
/// </summary>
public class DomainException(string message) : Exception(message);
