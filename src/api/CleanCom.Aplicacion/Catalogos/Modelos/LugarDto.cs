using CleanCom.Dominio.Catalogos;

namespace CleanCom.Aplicacion.Catalogos;

public record LugarDto(int Id, string Nombre, TipoLugar Tipo, string Direccion, double? Lat, double? Lng, string? Horario, bool Activo);
