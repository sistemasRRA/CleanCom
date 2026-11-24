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

## Cómo quedó (semana 6)

- **Indicadores:** sellos canjeados y cuponera canjeada (usados de otorgados) contra su meta (`Promociones.MetaCanjeSellos` y `MetaCanjeCuponera`), con "en meta" o "bajo la meta". Campañas, con las notificaciones.
- **Marcar como usado** escribiendo el código del beneficio.
- **Promociones iniciales (las de la demo):** al arrancar, si un programa no tiene ninguna promoción, el API captura los cinco sellos de Promociones Cruzadas (Edredón o cobija al 20%, Carga extra gratis, Docena con 15%, Diagnóstico + 10% y Sneaker sencillo a $100; vigencia hasta la entrega del pedido) y un cupón inactivo por línea en la cuponera ("Captura aquí el beneficio impreso en la cuponera."). Mientras haya cupones inactivos, la pestaña lo avisa.
- **Mecánica en texto:** cada promoción puede llevar la mecánica como la lee el invitado; si se deja vacía se arma con las reglas (tipo, valor, línea, mínimo, forma de pago, acumulable). Tipos de descuento: porcentaje, monto, artículo gratis y precio especial.
- **Pestañas Sellos y Cuponera:** promoción y condición, mecánica en texto ("20% de descuento en Tintorería · no acumulable"), "se da al cerrar" (sellos), vigencia, otorgados, usados, apartados, canje y, en sellos, su parte de los sellos otorgados con la marca "tope" al pasar de `Promociones.TopeSello`. "Nueva promoción" y "Editar" con las reglas estructuradas; una inactiva ya no se entrega.
- Reglas de entrega, apartado y uso en [reglas · Promociones](../../03-reglas-de-negocio.md#promociones).
- **Falta:** campañas (con las notificaciones), resultados en dinero (con el POS) y QR del código.

## Endpoints

- `GET /promociones` (indicadores y promociones con sus números), `POST /promociones`, `PUT /promociones/{id}` (hecho): Promociones View/Edit.
- `POST /beneficios/{codigo}/usar` (hecho): editar Promociones, Clientes (CRM) o Pedidos. Uno usado o vencido responde 400.
- `GET|POST /invitados/{id}/beneficios` (hecho): los beneficios del invitado y dar uno a mano (con aviso si pasa el tope).
- `POST /campanas`, `POST /campanas/{id}/previsualizar`, `POST /campanas/{id}/enviar` (con las notificaciones).

## Criterios de aceptación

- Un invitado con baja de promociones no recibe campañas.
- Un beneficio usado no se puede volver a usar.
