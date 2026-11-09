# Integraciones

Cada integración vive en `CleanCom.Infraestructura` detrás de una interfaz, para poder cambiar de proveedor y para simularla en pruebas. Las llaves van en variables de entorno.

## Google Maps Platform

| Servicio | Uso | Dónde |
| --- | --- | --- |
| Places Autocomplete | Capturar direcciones (oficina y app del invitado) | Web y app invitado |
| Geocoding | Coordenadas, colonia y CP de una dirección; limpieza en la migración | API |
| Maps JavaScript API | Panel del día, Rutas, ficha | Web |
| Maps SDK (vía `react-native-maps`) | Ruta del chofer, seguimiento del invitado | Apps |
| Routes API (Compute Routes) | Tiempos con tráfico, regreso estimado, trazo por calles | API |
| Route Optimization API (`optimizeTours`, un vehículo) | Orden de paradas con ventanas, viajes encadenados, comida, inicio y fin en la base | API |

- Se optimiza **una ruta a la vez** (SKU Single Vehicle Routing, el más barato).
- Restricciones que se envían: ventanas por turno u hora específica, duración de servicio por tipo de parada, pasos de viaje en orden, posiciones fijas, comida como pausa, regreso máximo.
- Navegación del chofer: se abre Google Maps o Waze con un deep link; no se usa la Navigation SDK.
- Poner tope de gasto mensual y alertas de facturación en Google Cloud.

## WhatsApp (Cloud API de Meta)

- Conexión directa con la Cloud API (sin intermediario). Requiere número nuevo, cuenta de Meta Business verificada y plantillas aprobadas. Ver [08-costos.md](08-costos.md).
- Plantillas actuales: `cleancom_cita_agendada`, `cleancom_chofer_en_camino`, `cleancom_prendas_en_proceso` (inactiva), `cleancom_entrega_confirmada`, más las que falten por evento.
- Webhook para estados (enviado, entregado, leído, fallido) y mensajes entrantes (bandeja de respuestas y bajas con "BAJA").
- Respuestas libres solo dentro de la ventana de 24 horas que abre el invitado al escribir.
- Plantillas de marketing separadas de las de utilidad.
- Respaldo: si el envío falla o la cuenta no está conectada, el mensaje queda "para mandar a mano" con botón para abrir WhatsApp Web, como hoy.

## Código de verificación (registro del invitado)

1. Canal principal: plantilla de autenticación de WhatsApp.
2. Respaldo: SMS con **Twilio Programmable SMS** si el invitado no tiene WhatsApp o el código no llega en 60 segundos.
3. El código lo genera y valida el API (6 dígitos, 10 minutos de vigencia, máximo 5 intentos, límite de envíos por número y por hora).
4. El proveedor de SMS es configurable.

## Notificaciones push

- Expo Notifications (FCM para Android, APNs para iOS).
- El API guarda el token de cada dispositivo por usuario.
- Oficina recibe avisos en la web con una campana (SignalR) y, opcionalmente, notificaciones del navegador.

## Correo

- Servicio transaccional (Azure Communication Services Email o SendGrid) para avisos de flota, resúmenes y restablecimiento de contraseña.
- Dominio propio con SPF y DKIM configurados.

## POS

- Fuente oficial de tickets, cobros y facturación.
- Hoy los tickets ya llegan del POS al sistema actual. **Pendiente:** confirmar el sistema y el mecanismo (API, base de datos o archivo). Ver [09-pendientes.md](09-pendientes.md).
- Lo que el API necesita recibir: tickets (folio, invitado o pedido, línea de servicio, prendas, monto, fecha), pagos (forma, monto, fecha) y facturación por periodo.
- Diseño: un `IProveedorPos` con un proceso de Hangfire que sincroniza cada pocos minutos, guarda la última marca de tiempo leída y no duplica tickets (folio único).
- Fase 2: el POS lee el código de cada beneficio para registrar canjes.

## BigQuery (fase 2)

Copia nocturna de tablas de hechos (pedidos, visitas, mensajes, valores de KPIs) para cruzar con HubSpot, pauta y nómina, y para costo por fuente.
