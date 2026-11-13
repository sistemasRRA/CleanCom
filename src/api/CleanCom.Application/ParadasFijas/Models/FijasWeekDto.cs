namespace CleanCom.Application.ParadasFijas;

/// <summary>"La semana de un vistazo": Monday to Saturday, by ruta.</summary>
public record FijasWeekDto(DateOnly From, DateOnly To, IReadOnlyList<FijasWeekRutaDto> Rutas, IReadOnlyList<FijasWeekDayDto> Days);
