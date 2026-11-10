namespace CleanCom.Aplicacion.Catalogos;

public record GuardarLineaServicioRequest(string Nombre, int? DiasProceso, bool Activo = true);
