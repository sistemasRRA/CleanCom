namespace CleanCom.Application.Access;

public record SessionUserDto(int Id, string FullName, string UserName, string Role, bool MustChangePassword, IReadOnlyList<string> Permissions);
