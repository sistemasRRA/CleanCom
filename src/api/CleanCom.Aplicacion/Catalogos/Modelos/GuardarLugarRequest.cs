using CleanCom.Dominio.Catalogos;

namespace CleanCom.Aplicacion.Catalogos;

public record GuardarLugarRequest(string Nombre, TipoLugar Tipo, string Direccion, double? Lat, double? Lng, string? Horario, bool Activo = true);
