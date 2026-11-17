namespace CleanCom.Application.Rutas;

/// <summary><c>PATCH /paradas/{id}</c>: move it (and its viaje) and/or pin it.</summary>
/// <param name="Position">New place in the ruta, from 1. A parada moved by hand stays fixed unless <paramref name="Fixed"/> is false.</param>
/// <param name="Fixed">Pin or release its position; optimizing keeps pinned paradas where they are.</param>
public record PatchParadaRequest(int? Position, bool? Fixed);
