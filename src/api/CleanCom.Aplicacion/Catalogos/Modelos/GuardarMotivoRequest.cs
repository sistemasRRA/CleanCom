using CleanCom.Dominio.Catalogos;

namespace CleanCom.Aplicacion.Catalogos;

public record GuardarMotivoRequest(TipoMotivo Tipo, string Texto, bool Activo = true);
