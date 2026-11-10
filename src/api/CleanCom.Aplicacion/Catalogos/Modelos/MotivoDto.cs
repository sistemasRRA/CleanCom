using CleanCom.Dominio.Catalogos;

namespace CleanCom.Aplicacion.Catalogos;

public record MotivoDto(int Id, TipoMotivo Tipo, string Texto, bool Activo);
