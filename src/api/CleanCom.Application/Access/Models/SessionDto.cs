namespace CleanCom.Application.Access;

public record SessionDto(string AccessToken, DateTimeOffset ExpiresAt, string RefreshToken, DateTimeOffset RefreshExpiresAt, SessionUserDto User);
