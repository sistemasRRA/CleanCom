namespace CleanCom.Aplicacion.Catalogos;

public record GuardarRutaRequest(string Codigo, string Nombre, string Color, int? CupoParadas, int? MetaRecolecciones, TimeOnly? RegresoMaximo, TimeOnly? RegresoMaximoSabado, bool Activo = true);
