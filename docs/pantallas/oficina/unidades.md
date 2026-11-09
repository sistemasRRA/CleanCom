# Unidades

**Ruta actual:** `/admin/unidades` · **Rol:** Administración

Flota: verificación, seguro, tarjeta de circulación, servicios, reparaciones y fallas.

## Se conserva

- Tarjeta por unidad: placa, marca, modelo y año, kilometraje con fecha, alertas (seguro vencido, verificación vencida o "toca este mes"), "sin servicio registrado", último y próximo servicio, gasto del año, seguro, tarjeta y verificación.
- Registrar evento, bitácora y datos.
- Kilometraje actualizado con salidas y regresos de ruta.

## Mejora

- Kilometraje real desde inicio y fin de turno en la app del chofer.
- Fallas reportadas desde la app con foto y gravedad; marcar unidad fuera de servicio.
- Servicio por kilómetros o por fecha, lo que llegue primero.
- Documentos adjuntos (póliza, tarjeta, comprobantes).
- Pagos recurrentes por unidad (tenencia o refrendo, crédito o arrendamiento, póliza en pagos) que suman al gasto del año.
- Costo por kilómetro.
- Aviso al asignar una unidad con documentos vencidos o falla abierta.

## Notificaciones

Por correo y notificación en la web (y push si el usuario tiene app). Calendario de avisos en [reglas de negocio · Flota](../../03-reglas-de-negocio.md#flota). Destinatarios configurables por evento, resumen semanal y recordatorio hasta que se registre el comprobante.

## Endpoints

- `GET /unidades`, `POST /unidades`, `PUT /unidades/{id}`.
- `POST /unidades/{id}/eventos`, `POST /unidades/{id}/documentos`, `POST /unidades/{id}/pagos`.
- `GET|PUT /avisos-flota/reglas`.
