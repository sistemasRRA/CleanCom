namespace CleanCom.Application.Access;

/// <param name="Phone">The invitado's cell phone; 10 digits (with or without +52).</param>
public record SendCodeRequest(string Phone);

/// <param name="SentTo">The number, half hidden ("55 **** 5678").</param>
/// <param name="ResendInSeconds">When a new code can be asked for.</param>
public record CodeSentDto(string SentTo, int ExpiresInMinutes, int ResendInSeconds);

/// <param name="Campaign">Source or campaign of the link the invitado came from ("Invitar a la app"), if any.</param>
public record VerifyCodeRequest(string Phone, string Code, string? Campaign = null);

/// <summary>The invitado's session after the code.</summary>
/// <param name="NewInvitado">The phone was not in the CRM: the app asks the name and "¿cómo nos conociste?" once.</param>
public record InvitadoSessionDto(SessionDto Session, bool NewInvitado);
