namespace CleanCom.Dominio.Tiempo;

/// <summary>
/// Hora del negocio. Todo corte y fecha de negocio se evalúa en hora de Ciudad de México;
/// en base de datos se guarda UTC. Se inyecta para poder probar los cortes.
/// </summary>
public interface IRelojNegocio
{
    /// <summary>Instante actual en UTC (lo que se guarda en base de datos).</summary>
    DateTimeOffset AhoraUtc { get; }

    /// <summary>Instante actual con el desfase de Ciudad de México.</summary>
    DateTimeOffset AhoraLocal { get; }

    /// <summary>Fecha de negocio de hoy en Ciudad de México.</summary>
    DateOnly Hoy { get; }
}
