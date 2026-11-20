# Panel del día

**Ruta actual:** `/logistica` · **Rol:** Administración

Resumen operativo del día: alertas, avance de cada ruta, mapa en vivo, carga de los próximos 7 días y últimas visitas.

## Se conserva

- Encabezado: fecha con navegación, salida 09:00, regreso 18:00 y meta de recolecciones por ruta.
- Alertas: paradas de días pasados sin cerrar (con "Reprogramar"), pedidos 100% listos sin fecha de entrega ("Confirmar entregas"), pedidos con 3 días o más en planta ("Ver planta").
- Tarjeta por ruta: zonas, recolecciones contra meta, estado (sin salir, en ruta, de regreso), chofer, unidad, paradas, km estimados, regreso estimado, GPS, cobrado y "Ver ruta".
- Mapa del día con color por ruta y estado (en camino, realizada, no realizada) y chofer en vivo.
- Carga de los próximos 7 días contra el cupo de 14 por ruta.
- Últimas visitas con evidencia y "Ver evidencias".

## Mejora

- Franja de indicadores: recolecciones, entregas, cobrado de referencia, % de llegadas a tiempo y pedidos en planta.
- Bandeja de pendientes compacta: un contador por tipo de alerta que abre su lista.
- Flota: solo un contador ("3 unidades con pendientes") que lleva a Unidades; el detalle ya no ocupa el panel.
- Tarjetas de ruta accionables: asignar chofer y unidad desde la tarjeta; aviso de atraso, horas extra o sin GPS; avance separado en recolecciones y entregas.
- Mapa en vivo por SignalR con ruta planeada contra recorrida y paradas sin asignar que se pueden arrastrar a una ruta.
- Carga semanal como barras contra el cupo, con el día excedido resaltado y clic para planear ese día.

## Endpoints

- `GET /panel?fecha=` (hecho): indicadores, pendientes con sus listas, contador de flota, una tarjeta por ruta (zonas, chofer, unidad, avance de recolecciones contra meta y de entregas, estado, km y regreso estimados, última ubicación, cobrado y alertas de atraso, horas extra, sin GPS y sin unidad), carga de 7 días y últimas visitas. Reglas en [reglas de negocio · Panel del día](../../03-reglas-de-negocio.md#panel-del-día). Chofer y unidad se asignan con `PUT /rutas/{rutaId}/dias/{fecha}/chofer` y `/unidad`.
- `GET /rutas/dia?fecha=` rutas con paradas para el mapa.
- `GET /agenda/carga?desde=&dias=7`.
- `GET /ubicaciones?fecha=` (hecho): última posición conocida de cada ruta del día (`live: false` cuando viene del historial, por ejemplo después de reiniciar el API).
- `GET /rutas/dia/{id}/recorrido` (hecho): puntos del recorrido en orden, para comparar contra la ruta planeada.
- Hub SignalR `operacion` en `/hubs/operacion` (hecho): eventos `ubicacion` (misma forma que `GET /ubicaciones`) y `parada` (`rutaDiaId`, `date`, `paradaId`, `state`) cuando el chofer marca en camino, llegué, cerrada o no realizada. El token va en `access_token` de la URL. Permiso: ver Panel del día o Rutas.

## Criterios de aceptación

- El panel se actualiza sin recargar cuando un chofer cierra una visita.
- Los contadores de alertas coinciden con las listas que abren.
- La carga de 7 días incluye las paradas fijas.
