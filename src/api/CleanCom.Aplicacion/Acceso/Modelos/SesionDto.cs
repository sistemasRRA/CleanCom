namespace CleanCom.Aplicacion.Acceso;

public record SesionDto(string TokenAcceso, DateTimeOffset ExpiraEn, string TokenRenovacion, DateTimeOffset RenovacionExpiraEn, UsuarioSesionDto Usuario);
