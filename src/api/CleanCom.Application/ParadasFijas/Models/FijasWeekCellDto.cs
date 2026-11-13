namespace CleanCom.Application.ParadasFijas;

/// <param name="FijasCupo">Places the paradas fijas take that day (one per paso).</param>
public record FijasWeekCellDto(int RutaId, int Cupo, int FijasCupo, IReadOnlyList<FijasWeekItemDto> Items);
