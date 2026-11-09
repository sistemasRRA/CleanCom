# WhatsApp y metas

**Ruta actual:** `/admin/mensajes` · **Rol:** Administración

Plantillas de mensajes por evento y metas de los KPIs.

## Se conserva

- Aviso de que WhatsApp no está conectado y los mensajes se mandan con un clic hasta cargar las llaves de Meta.
- Plantilla por evento: nombre en Meta, texto con variables `{{1}}`, `{{2}}`…, variables documentadas, activa, guardar.

| Evento | Plantilla | Estado actual |
| --- | --- | --- |
| Al agendar la cita | `cleancom_cita_agendada` | Activa |
| Cuando el chofer sale hacia el domicilio | `cleancom_chofer_en_camino` | Activa |
| Cuando la ropa llega a la tienda 7clean | `cleancom_prendas_en_proceso` | Inactiva |
| Al confirmar la fecha de entrega | `cleancom_entrega_confirmada` | Activa |

- Metas de KPIs: indicador (área, más o menos es mejor, unidad), periodo, responsable, rojo, verde, súper verde.

## Mejora

- Conexión directa con la Cloud API de Meta; estado de cada plantilla leído de Meta.
- Texto de WhatsApp y texto de push por evento.
- Vista previa con datos de ejemplo.
- Bandeja de respuestas ligada a invitado y pedido, para contestar dentro de la ventana de 24 horas.
- Bajas automáticas con "BAJA".
- Plantillas de marketing separadas para campañas.
- Metas con vigencia, responsable elegido de la lista de usuarios y validación del orden rojo, verde, súper verde.

## Endpoints

- `GET|PUT /plantillas`, `POST /plantillas/{id}/previsualizar`, `POST /plantillas/sincronizar-meta`.
- `GET /bandeja`, `POST /bandeja/{id}/responder`.
- `POST /webhooks/whatsapp` (estados y mensajes entrantes).
- `GET|PUT /metas`.
