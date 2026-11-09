# Promociones

**Ruta actual:** `/logistica/promociones` · **Rol:** Administración

Sellos de Promociones Cruzadas, Cuponera de Bienvenida y campañas.

## Se conserva

- Indicadores: sellos canjeados (meta 40% o más), cuponera canjeada (meta 30% o más) y campañas enviadas y canjeadas; sello más dado contra el máximo de 30%.
- Enviar campaña: campaña, ruta, estatus del CRM (Le toca, En riesgo, Inactivo, Activo, Nuevo, Prospecto), conteo de invitados y envío; excluye a quien pidió no ser contactado o ya tiene el beneficio.
- Sellos: promoción, línea, mecánica, condición, vigencia, otorgados, usados, canje, Editar. Ejemplos: "Edredón o cobija al 20%" (tintorería), "Carga extra gratis" (lavandería).
- Cuponera por línea (tintorería, lavandería, planchaduría, sastrería, calzado), vigencia de 60 días, activación por cupón.
- Campañas con las mismas métricas; "Nueva campaña".
- El chofer ve los beneficios del invitado y los marca al usarse.

## Mejora

- Reglas estructuradas: tipo de descuento, valor, línea, mínimo, forma de pago, acumulable, condición.
- Código único por beneficio (QR o texto) para que no se use dos veces y para que el POS lo lea en fase 2.
- Entrega automática de la cuponera al cerrar el primer servicio de un invitado nuevo.
- Campañas por canal (push gratis o WhatsApp de marketing) y programadas por fecha y hora; envío automático por la API en lugar de uno por uno.
- Resultados en dinero: ventas generadas, costo del descuento y costo de los mensajes.
- Alerta cuando un sello rebasa el 30% de los otorgados.

## Endpoints

- `GET /promociones`, `POST /promociones`, `PUT /promociones/{id}`.
- `POST /campanas`, `POST /campanas/{id}/previsualizar`, `POST /campanas/{id}/enviar`.
- `POST /beneficios/{codigo}/usar`.

## Criterios de aceptación

- Un invitado con baja de promociones no recibe campañas.
- Un beneficio usado no se puede volver a usar.
