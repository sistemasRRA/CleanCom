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

API y pantalla hechos en la semana 5 (`/unidades`): tarjetas con avisos, ficha con bitácora, documentos (subir y ver archivo) y pagos recurrentes ("Registrar pago" mueve la fecha), alta y edición, fuera de servicio, baja lógica y las reglas de aviso.

- `GET /unidades` (`?includeInactive=`): tarjetas con alertas, último y próximo servicio, gasto del año, costo por km y `withPending` (para el contador del Panel del día). `GET /unidades/{id}`: tarjeta, documentos, bitácora y pagos recurrentes.
- `POST /unidades`, `PUT /unidades/{id}` (datos, fuera de servicio, exenta de verificación y baja lógica con `active: false`). La placa se guarda sin espacios ni guiones, en mayúsculas, y no se repite entre unidades activas.
- `POST /unidades/{id}/eventos`: servicio, reparación, falla (con gravedad), pago (de un pago recurrente o suelto) o kilometraje. Un kilometraje mayor actualiza el de la unidad. `POST /unidades/{id}/eventos/{eventoId}/resolver` cierra una falla.
- `POST /unidades/{id}/documentos` (multipart): seguro y tarjeta con su vigencia, verificación con la fecha en que se hizo, comprobantes. Archivo opcional: PDF, JPG, PNG o WebP hasta 15 MB, en disco. `GET /unidades/{id}/documentos/{documentoId}/archivo`, `DELETE` (baja lógica).
- `POST /unidades/{id}/pagos`, `PUT /unidades/{id}/pagos/{pagoId}`: pagos recurrentes.
- `GET|PUT /avisos-flota/reglas`: días de aviso, destinatarios y canales por evento.
- `PUT /rutas/{rutaId}/dias/{fecha}/unidad` (permiso de Rutas): asigna la unidad del día; responde 409 hasta confirmar (`confirm: true`) si tiene pendientes. La capacidad de prendas de la unidad se usa en el aviso de la ruta.
- Reglas de cálculo en [reglas de negocio · Flota](../../03-reglas-de-negocio.md#flota).
