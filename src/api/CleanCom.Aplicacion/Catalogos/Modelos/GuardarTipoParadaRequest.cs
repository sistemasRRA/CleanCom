namespace CleanCom.Aplicacion.Catalogos;

public record GuardarTipoParadaRequest(string Nombre, string? CamposRequeridos, bool Activo = true);
