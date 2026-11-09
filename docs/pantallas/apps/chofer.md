# App del chofer

**Plataforma:** Expo (React Native), diseñada primero para **tableta** (lista y mapa lado a lado) y usable en teléfono. Android y iOS desde el mismo código. **Rol:** Chofer.

## Flujo del turno

1. **Inicio de turno:** inicia sesión con su usuario (sesión por turno en tabletas compartidas), ve su ruta y unidad del día y registra kilometraje inicial (foto del tablero opcional). Aviso si la unidad tiene documentos vencidos o falla abierta.
2. **Ruta del día:** lista ordenada y mapa con hora estimada, turno, tipo de parada (recolección, entrega, convenio, insumos, traspaso), paso de viaje, notas y beneficios del invitado. Los cambios de oficina llegan por notificación y la ruta se actualiza.
3. **En camino:** abre Google Maps o Waze y avisa al invitado con su hora estimada y liga de seguimiento.
4. **Llegué:** registra la hora real; se sugiere al entrar al radio de 300 m.
5. **Cerrar la visita** según el tipo:
    - Recolección: prendas aproximadas, foto, notas, beneficios usados.
    - Entrega: foto, firma del invitado, forma y monto de pago de referencia.
    - Traspaso o insumos: confirmar lo cargado o entregado.
    - No realizada: motivo de catálogo y foto; el pedido se reprograma y se avisa a oficina.
6. **Comida:** pausa de 1 hora; al regresar se recalcula la hora estimada.
7. **Fin de turno:** regreso a Leones, kilometraje final y declaración de cobros de referencia para el corte de caja.

## Otras funciones

- Reportar falla de la unidad con foto y gravedad.
- Solicitar a oficina un cambio de orden (el chofer no reordena; si `Visita.OrdenObligatorio` está activo, cierra las paradas en el orden asignado).
- Llamar o escribir al invitado desde la parada.

## Requisitos técnicos

- **Sin señal:** cada acción (estado, foto, firma, kilometraje) se guarda en SQLite con un `IdLocal` y se sincroniza al recuperar conexión; el API no duplica reenvíos.
- **Ubicación en segundo plano solo durante el turno**, cada 30 a 60 segundos; se detiene al cerrar el turno. Texto de permiso claro para la revisión de las tiendas.
- **Fotos comprimidas** en el dispositivo antes de subir.
- **Botones grandes y pocos pasos:** cerrar una visita en menos de un minuto.
- Foto obligatoria según configuración.

## Endpoints

- `POST /chofer/turno/iniciar`, `POST /chofer/turno/cerrar`.
- `GET /chofer/ruta?fecha=`.
- `POST /chofer/paradas/{id}/en-camino`, `/llegue`, `/cerrar`, `/no-realizada`.
- `POST /chofer/evidencias` (multipart), `POST /chofer/ubicacion`, `POST /chofer/sincronizar` (lote de la cola offline).
- `POST /chofer/fallas`, `POST /chofer/solicitudes-cambio`.

## Criterios de aceptación

- Una visita cerrada sin señal aparece en oficina en cuanto vuelve la conexión, sin duplicarse.
- "En camino" envía el aviso al invitado una sola vez.
- La ubicación deja de enviarse al cerrar el turno.
