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

## Endpoints

- `GET /planta/tickets?vista=`, `POST /planta/tickets/{folio}/listo`, `POST /planta/comentarios`.

## Criterios de aceptación

- Escanear el último ticket pendiente de un pedido lo mueve a Listo y avisa a logística.
- Un folio inexistente muestra un error claro sin romper la captura continua.
