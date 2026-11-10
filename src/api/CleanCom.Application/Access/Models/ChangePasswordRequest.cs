namespace CleanCom.Application.Access;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
