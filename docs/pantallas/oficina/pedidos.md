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

## Cómo quedó (semana 4, primera parte)

- **Tablero** (`GET /pedidos/tablero?ruta=&q=&atrasados=`): columnas Agendado a Entregado (últimos 7 días); Cancelado no aparece. Tarjeta con ruta, folio (por ahora el id), invitado, prendas, fecha de recolección o de entrega, días en tienda, atraso con semáforo (amarillo el día que vence, rojo vencido) e intentos fallidos. Búsqueda por invitado, folio o teléfono; filtro "Atrasados e intentos fallidos" con contador.
- **Atraso:** fecha de recolección o de entrega vencida, o más días en planta que los de proceso de su línea (la más larga) o `Planta.DiasProceso`. Los días permitidos por etapa siguen pendientes.
- **Historial** (`GET /pedidos/{id}`): cada cambio de etapa (automático o a mano, con motivo), entrega programada o cambiada, intentos fallidos y comentarios, con quién y cuándo. También las visitas del pedido.
- **Cambiar etapa** (`POST /pedidos/{id}/etapa`): avanzar es libre; regresar, entregar a mano o cancelar pide motivo. Cancelar quita sus visitas pendientes de las rutas; un cancelado no se reabre. En lugar de arrastrar la tarjeta se elige la etapa en su ficha.
- **Programar entrega** (`GET /pedidos/{id}/fechas-entrega`, `POST /pedidos/{id}/programar-entrega`): días con cupo de su zona con el mismo motor de la agenda; la entrega ocupa cupo. Desde En planta (entrega parcial), Listo o para cambiar la fecha; una fecha nueva reemplaza la entrega pendiente. Pasa a Entrega confirmada.
- **No se entregó** (`POST /pedidos/{id}/no-entregado`): motivo de catálogo o texto; la visita queda "no realizada", suma un intento y se reprograma al siguiente día con cupo desde mañana, sin límite. El aviso a oficina e invitado llega con las notificaciones (semana 6).
- **Automático:** al marcar el chofer "en camino" a una entrega, el pedido pasa a En camino. Cerrar recolección y entrega llega con el cierre de visita en la app. En planta y Listo los mueven los tickets (ver [Tickets en planta](tickets-en-planta.md)); la tarjeta muestra "2/3 tickets listos" y la ficha, los tickets y los comentarios de atraso de planta.

## Endpoints

- `GET /pedidos/tablero?ruta=&q=&atrasados=`, `GET /pedidos/{id}`, `GET /pedidos?from=&to=&invitadoId=`.
- `POST /pedidos/{id}/etapa` (con motivo cuando aplica), `GET /pedidos/{id}/fechas-entrega`, `POST /pedidos/{id}/programar-entrega`, `POST /pedidos/{id}/no-entregado`, `POST /pedidos/{id}/comentarios`.
- `GET /mensajes?filtros=`, `POST /mensajes/{id}/reenviar`, `POST /mensajes/{id}/marcar-manual`.

## Criterios de aceptación

- Cerrar una recolección en la app mueve la tarjeta a Recolectado sin recargar.
- Regresar una tarjeta sin motivo no es posible.
- Un pedido marcado "no se entregó" aparece en el filtro de intentos fallidos con su nueva fecha.
