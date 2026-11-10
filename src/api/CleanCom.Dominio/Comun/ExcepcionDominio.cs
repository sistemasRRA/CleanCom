namespace CleanCom.Dominio.Comun;

/// <summary>
/// Una regla de negocio no se cumple. El mensaje se muestra al usuario, en español.
/// </summary>
public class ExcepcionDominio(string mensaje) : Exception(mensaje);
