# Lo que se necesita y costos

Con el hosting en el VPS propio, el costo mensual de operación con 2 rutas queda alrededor de **30 a 150 USD**, casi todo por WhatsApp y Google Maps, más 99 USD al año de Apple y 25 USD una sola vez de Google Play. Cifras aproximadas a noviembre de 2026; se confirman al abrir cada cuenta.

## WhatsApp

- **Línea telefónica nueva:** propia, que reciba SMS o llamada para verificarse y sin cuenta de WhatsApp. Puede ser celular o fija. Si ya tiene WhatsApp hay que borrar esa cuenta antes; conectada a la API ya no se puede usar en la app de WhatsApp. ([requisitos](https://www.zoko.io/learning-article/what-phone-numbers-can-i-use))
- **Cuenta de Meta Business verificada:** razón social, RFC, comprobante de domicilio y sitio web con dominio propio.
- **Plantillas aprobadas** para cada aviso y campaña.

| Tipo de mensaje (México, desde el 1 de octubre de 2026) | Precio por mensaje entregado | Uso |
| --- | --- | --- |
| Utilidad | 0.0085 USD (0.16 MXN) | Avisos de cita, chofer en camino, entrega |
| Autenticación | 0.0085 USD (0.16 MXN) | Código de registro |
| Marketing | 0.0397 USD (0.73 MXN) | Campañas y reactivación |
| Servicio (respuestas al invitado) | Gratis los primeros 1,000 al mes por número | Bandeja de respuestas |

Fuente: [precios de WhatsApp Business en México](https://www.goeva.ai/en/resources/whatsapp-business-api-pricing-mexico). Con unos 150 pedidos al mes y 5 avisos cada uno, más 400 mensajes de campaña: **unos 25 USD al mes**. Las notificaciones push no cuestan.

## SMS

Solo para el código de verificación cuando no llega por WhatsApp.

| Opción | Precio | Cuándo conviene |
| --- | --- | --- |
| Código por WhatsApp (Meta) | 0.0085 USD | Primera opción |
| Twilio Programmable SMS | 0.1819 USD por SMS a México + número rentado de 6.50 a 15 USD al mes ([precios](https://twilio.com/en-us/sms/pricing/mx)) | Respaldo recomendado |
| Twilio Verify | 0.05 USD por verificación + costo del canal ([precios](https://twilio.com/verify/pricing)) | Si se prefiere que Twilio administre códigos |
| Plivo, Telnyx, Infobip | Por cotizar | Si el volumen creciera |

Con unos 40 registros al mes y 1 de cada 4 por SMS: unos 2 USD más el número rentado.

## Tiendas de apps

| Tienda | Costo | Requisitos |
| --- | --- | --- |
| Apple App Store | 99 USD al año | Cuenta de empresa con número D-U-N-S (gratuito, de días a dos semanas) |
| Google Play | 25 USD una vez | Cuenta de organización verificada |

Las dos apps se publican con la misma cuenta. No hay comisión de las tiendas porque los servicios son físicos, no compras digitales.

## Google Maps Platform

Uso gratuito mensual por servicio: 10,000 solicitudes en los básicos, 5,000 en Pro y 1,000 en Enterprise ([límites](https://developers.google.com/maps/billing-and-pricing/pricing-categories)).

| Servicio | Costo esperado |
| --- | --- |
| Places Autocomplete y Geocoding | Dentro del uso gratuito |
| Mapas en web y apps | Dentro del uso gratuito al volumen actual |
| Routes | Dentro del uso gratuito |
| Route Optimization (una ruta a la vez) | Unos 10 USD por cada 1,000 paradas optimizadas ([precios](https://blog.afi.io/blog/gmpro-google-maps-platform-route-optimization-api/)); de 0 a 100 USD al mes según cuántas veces se recalcule |

## Servidor

- VPS propio: sin costo adicional.
- SQL Server Express: gratuito (10 GB por base de datos).
- Certificado SSL: gratuito con Let's Encrypt.
- Respaldo externo: almacenamiento de pocos dólares al mes.

## Otros

| Concepto | Costo |
| --- | --- |
| Notificaciones push | Sin costo |
| Correo transaccional | Gratis o pocos dólares al mes |
| Tabletas para choferes | Según modelo (Android de gama media) |
| Línea telefónica para WhatsApp | Plan básico de celular o línea fija |
