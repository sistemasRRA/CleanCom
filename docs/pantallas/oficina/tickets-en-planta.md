# Tickets en planta

**Ruta actual:** `/planta` · **Rol:** Administración (pensada para tableta en planta)

Planta marca cada ticket cuando queda listo. Cuando todos los tickets de un pedido están listos, el pedido pasa a Listo.

## Se conserva

- Indicadores: tickets en proceso, pedidos 100% listos, pedidos parciales, con entrega hoy o atrasada, con 3 días o más en planta.
- Captura de folio por escáner o teclado con Enter para marcarlo listo.
- Vistas "En proceso" y "Listos sin entregar".
- Tarjeta por pedido: ruta, invitado, folio, días en planta, entrega atrasada, tickets (folio, línea, prendas) con botón Listo.
- "¿Por qué se atrasa? Comentar", visible para logística en Pedidos.

## Mejora

- Vista para tableta con botones grandes y escáner con la cámara.
- Orden por urgencia: entrega hoy o atrasada primero, luego días en planta.
- Días de proceso por línea de servicio.
- Motivos de atraso de catálogo además del comentario libre.
- Aviso inmediato a logística cuando un pedido queda 100% listo o se registra un atraso.
- Tickets recibidos del POS automáticamente (ver integraciones).

## Cómo quedó (semana 4, segunda parte)

- **Captura manual mientras se conecta el POS** (`POST /planta/tickets`): pedido (folio), folio del ticket, línea de servicio, prendas y monto opcional. El folio es único entre tickets activos y se compara sin espacios y en mayúsculas. El primer ticket pasa el pedido a En planta (desde Agendado, Recolectado o En planta). Un pedido Listo o más adelante no recibe tickets: se regresa a En planta en Pedidos, con motivo.
- **Marcar listo** (`POST /planta/tickets/{folio}/listo`): caja de captura continua para escáner o teclado con Enter; un error (folio inexistente o ya listo) se muestra y la caja sigue lista para el siguiente. El último ticket listo pasa el pedido a Listo. Después de una entrega parcial el pedido ya no regresa a Listo.
- **Indicadores y vistas** (`GET /planta/tickets?vista=EnProceso|Listos`): tickets en proceso, pedidos 100% listos, parciales, con entrega hoy o atrasada y con `Planta.DiasProceso` días o más en planta. Orden: entrega hoy o atrasada primero, luego más días en planta.
- **Atraso por ticket:** contra los días de proceso de su línea (o `Planta.DiasProceso`), contados desde que llegó el ticket; amarillo el día que vence.
- **¿Por qué se atrasa?** (`POST /planta/comentarios`): motivo del catálogo "atraso en planta" y/o texto; queda en el historial del pedido y se ve en Pedidos.
- **Baja de un ticket capturado por error** (`POST /planta/tickets/{id}/baja`): solo en proceso y con motivo; es baja lógica y queda en el historial.
- En Pedidos, la tarjeta muestra "2/3 tickets listos" y la ficha la lista de tickets.
- Pendiente: escáner con la cámara de la tableta (hoy, escáner de código de barras o teclado), aviso a logística (semana 6, notificaciones) y tickets automáticos del POS.

## Endpoints

- `GET /planta/tickets?vista=`, `POST /planta/tickets`, `POST /planta/tickets/{folio}/listo`, `POST /planta/tickets/{id}/baja`, `POST /planta/comentarios`.

## Criterios de aceptación

- Escanear el último ticket pendiente de un pedido lo mueve a Listo y avisa a logística.
- Un folio inexistente muestra un error claro sin romper la captura continua.
