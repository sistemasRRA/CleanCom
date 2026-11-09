# Pedidos

**Ruta actual:** `/logistica/pedidos` · **Rol:** Administración

Tablero del camino de cada pedido, de la recolección a la entrega, y bitácora de mensajes de WhatsApp.

## Se conserva

- Tablero con columnas Agendado, Recolectado, En planta, Listo, Entrega confirmada, En camino y Entregado (últimos 7 días).
- Arrastrar una tarjeta para cambiar de etapa; regresarla o entregarla a mano pide motivo y queda en el historial.
- Tarjeta: ruta, invitado, folio, prendas, fecha de recolección o entrega, días en tienda, atraso ("Atrasado 6 días · tocaba el mar 27 oct"), tickets con estado y "100% listo".
- Acciones: programar entrega, cambiar fecha, no se entregó, tickets, en tienda, comentar, historial, seguimiento, cliente, cancelar.
- "Listo" se llena solo cuando planta marca todos los tickets.
- Búsqueda por invitado, folio o ticket; filtro "Atrasados e intentos fallidos" con contador.
- Pestaña "Mensajes de WhatsApp": cuándo, invitado y folio, mensaje, estado ("Para mandar a mano"), "Abrir WhatsApp" y "Ya lo mandé".
- Link de seguimiento público para el invitado.

## Mejora

- Etapas automáticas con los eventos del chofer (ver reglas de Pedidos); arrastrar queda para excepciones.
- Avisos automáticos por etapa por push o WhatsApp.
- Bitácora de mensajes con estado real (enviado, entregado, leído, fallido); reenviar o mandar a mano los fallidos.
- Filtros combinables (ruta, etapa, atrasados, intentos fallidos, convenio, fechas) y vista de lista exportable.
- Atraso configurable por etapa con semáforo antes de vencerse.
- No se entregó: motivo y foto desde la app, reprogramación a la siguiente fecha de la zona y aviso a oficina, sin límite de intentos.
- Historial completo de entregados en la vista de lista.

## Endpoints

- `GET /pedidos/tablero?filtros=`, `GET /pedidos?filtros=&pagina=`.
- `POST /pedidos/{id}/etapa` (con motivo cuando aplica), `POST /pedidos/{id}/programar-entrega`, `POST /pedidos/{id}/no-entregado`.
- `GET /mensajes?filtros=`, `POST /mensajes/{id}/reenviar`, `POST /mensajes/{id}/marcar-manual`.

## Criterios de aceptación

- Cerrar una recolección en la app mueve la tarjeta a Recolectado sin recargar.
- Regresar una tarjeta sin motivo no es posible.
- Un pedido marcado "no se entregó" aparece en el filtro de intentos fallidos con su nueva fecha.
