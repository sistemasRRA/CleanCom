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

## Cómo quedó (semana 6, modo de prueba)

- **Aviso** de que WhatsApp no está conectado: cada aviso queda en "Por mandar" y se manda con un clic; el push se registra sin enviarse.
- **Por mandar:** fecha, invitado y teléfono, aviso y pedido, texto, canal y estado; "Abrir WhatsApp" con el texto escrito y "Ya lo mandé".
- **Historial:** los últimos 200 avisos con su estado (por enviar, enviado, simulado, por mandar a mano, mandado a mano, falló, no se avisó y por qué).
- **Plantillas:** una por evento con sus variables documentadas, nombre en Meta, texto de WhatsApp, título y texto del push y activa; al editar, vista previa con datos de ejemplo. No deja usar una variable que el evento no tiene.
- **Campana de oficina** (en el menú): lo que el invitado hace en la app (pedido nuevo, cambio de día, cancelación), con liga al pedido y "Atendido".
- **Metas (semana 7):** por indicador, área, si más o menos es mejor, a qué aplica (toda la empresa o una ruta), periodo de la meta, rojo (opcional), verde, súper verde (opcional), responsable (usuario activo de oficina; ni choferes ni invitados) y desde cuándo aplica; aviso de la próxima meta programada. Carga inicial: las metas de la hoja "KPI's 2026 CleanCom" (columnas 1Q 2026), vigentes desde el 1 de enero de 2026. Al cambiar: valida el orden, la fecha no puede ser anterior a hoy y muestra el historial de la meta (quién la capturó); una meta programada se puede quitar.
- **Falta:** conexión con Meta (estado de plantillas, webhook, bandeja de respuestas y "BAJA"), plantillas de marketing y campañas.

## Endpoints

- Hechos: `GET /avisos?estado=&pedidoId=` (ver WhatsApp y metas o Pedidos), `POST /avisos/{id}/mandado` (editar WhatsApp y metas o Pedidos), `GET /plantillas`, `PUT /plantillas/{id}`, `POST /plantillas/{id}/previsualizar` (WhatsApp y metas), `GET /avisos-oficina`, `POST /avisos-oficina/{id}/atendido` (ver Agendar, Pedidos o Clientes).
- Por hacer:

- `GET|PUT /plantillas`, `POST /plantillas/{id}/previsualizar`, `POST /plantillas/sincronizar-meta`.
- `GET /bandeja`, `POST /bandeja/{id}/responder`.
- `POST /webhooks/whatsapp` (estados y mensajes entrantes).
- Metas (hecho): `GET /metas` (ver WhatsApp y metas o KPIs), `PUT /metas/{clave}`, `DELETE /metas/{clave}/{id}` (editar WhatsApp y metas).
