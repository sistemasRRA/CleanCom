# Vista del chofer (vista previa en oficina)

**Ruta actual:** `/chofer` · **Rol:** Administración

La vista del chofer pasa a la app móvil ([../apps/chofer.md](../apps/chofer.md)). En la web de oficina se conserva una vista previa de solo lectura para ver exactamente lo que ve cada chofer.

## Se conserva

- Selector de ruta (F, K) y fecha, "Ver" y "Volver al panel".
- Mensaje "Sin ruta asignada" cuando logística no ha asignado la ruta.

## Mejora

- Muestra el estado en vivo de cada parada y la última ubicación del chofer; en gris cuando lleva `Gps.SinSenalMinutos` sin reportar.

## Endpoints

- `GET /chofer/vista?ruta=&fecha=` (semana 5, hecho; permiso Vista del chofer: View): el mismo cálculo que `GET /chofer/ruta` de la app (paradas, orden, horas, estado, siguiente) para el chofer de esa ruta ese día (el asignado o el de ruta habitual), más su última ubicación. Sin chofer avisa "Sin ruta asignada".
- Tiempo real con el hub `operacion` (`ubicacion` mueve al chofer en el mapa; `parada` recarga la vista). Ver Panel del día o Rutas o Vista del chofer abre el hub (política `SeesOperacion`).


