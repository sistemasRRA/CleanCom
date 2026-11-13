# Plan de trabajo

12 semanas: 8 de desarrollo en tres frentes en paralelo y 4 de pruebas, piloto y publicación. Supone desarrollo de tiempo completo (backend, web y móvil) con Claude Code.

| Semana | Backend y base de datos | Web de oficina | Apps móviles |
| --- | --- | --- | --- |
| 1 | Modelo de datos, acceso y roles, catálogos, preparación del VPS y despliegue automático | Sistema de diseño, menú por rol, inicio de sesión | Estructura de las dos apps, inicio de sesión |
| 2 | Motor de agenda (zonas, cortes, cupos), invitados (la importación del Excel máster se canceló: los catálogos se capturan en Operación) | Agendar, Clientes (CRM) y ficha | Chofer: ruta del día |
| 3 | Rutas y optimización con Google, paradas fijas y viajes | Rutas, Paradas fijas, Operación | Chofer: navegación, en camino y llegué |
| 4 | Pedidos y etapas, tickets, conexión con el POS | Pedidos, Tickets en planta | Chofer: cerrar visita, fotos, firma, sin señal |
| 5 | Tiempo real, GPS, flota y sus notificaciones | Panel del día, mapa en vivo, Unidades | Invitado: registro con código, pedir servicio, seguimiento |
| 6 | WhatsApp, push, correo, promociones | WhatsApp y metas, Promociones, Usuarios | Invitado: beneficios, cambios y cancelación, chofer en el mapa |
| 7 | KPIs, clientes y fuentes, caja, migración completa | KPIs, Clientes y fuentes, Evidencias y caja | Chofer: turno y kilometraje, fallas de unidad; ajustes en tableta |
| 8 | Integración de punta a punta y cierre de funciones | Correcciones | Versiones de prueba (TestFlight y prueba interna de Google Play) |
| 9 y 10 | Pruebas internas y piloto con la Ruta F en paralelo al sistema actual | | |
| 11 | Ajustes del piloto, Ruta K entra al piloto, envío a revisión de App Store y Google Play | | |
| 12 | Migración final de datos, capacitación, salida en vivo y publicación en tiendas | | |

## Hitos

| Fin de semana | Hito |
| --- | --- |
| 4 | Agenda, rutas, pedidos y app del chofer funcionando con datos de prueba |
| 8 | Todo el alcance listo y apps en prueba en tabletas reales |
| 10 | Piloto de la Ruta F aprobado |
| 12 | Salida en vivo y apps publicadas |

## Tareas de la semana 1 que no son de código

Tardan en resolverse y bloquean semanas posteriores:

- Verificación de la empresa en Meta Business y alta del número de WhatsApp.
- Cuenta de Apple Developer para empresa (número D-U-N-S) y cuenta de Google Play.
- Facturación de Google Cloud con llaves de Maps y tope de gasto.
- Cuenta de Twilio para SMS de respaldo.
- Acceso o documentación del POS.
- Respaldo de datos del sistema actual (el Excel máster ya no se usa).
- Revisión del VPS (capacidad, sistema operativo, accesos).

## Orden sugerido para Claude Code

Construir de adentro hacia afuera: primero entidades y reglas del dominio con sus pruebas (`docs/03-reglas-de-negocio.md`), luego casos de uso y endpoints, y al final las pantallas. Empezar por el motor de agenda porque lo usan oficina y la app del invitado.
