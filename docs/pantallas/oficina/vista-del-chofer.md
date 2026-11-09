# Vista del chofer (vista previa en oficina)

**Ruta actual:** `/chofer` · **Rol:** Administración

La vista del chofer pasa a la app móvil ([../apps/chofer.md](../apps/chofer.md)). En la web de oficina se conserva una vista previa de solo lectura para ver exactamente lo que ve cada chofer.

## Se conserva

- Selector de ruta (F, K) y fecha, "Ver" y "Volver al panel".
- Mensaje "Sin ruta asignada" cuando logística no ha asignado la ruta.

## Mejora

- Muestra el estado en vivo de cada parada y la última ubicación del chofer.

## Endpoints

- `GET /chofer/ruta?ruta=&fecha=` (el mismo que usa la app).
