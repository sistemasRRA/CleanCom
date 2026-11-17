# App del chofer

**Plataforma:** Expo (React Native), **responsiva**: tableta y celular, Android y iOS desde el mismo código. **Rol:** Chofer.

## Diseño responsivo

Cada pantalla se decide con `useLayout()` de `src/mobile/shared` (`isTablet`, `landscape`, `gutter`), nunca con medidas fijas.

| Elemento | Tableta horizontal | Celular o vertical |
| --- | --- | --- |
| Ruta del día | Lista (40 %) y mapa lado a lado | Pestañas Lista / Mapa, o mapa con la lista en un panel deslizable |
| Detalle de la parada | Junto a la lista | Pantalla propia |
| Firma del invitado | En la misma pantalla | Pantalla completa en horizontal |
| Barra superior | Logo completo | Logo corto |

- Botones con la medida mínima `touch`, sin tablas anchas.
- Cada pantalla se revisa en un celular chico (iPhone SE o equivalente) y en una tableta.

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
- `GET /chofer/ruta?fecha=` (semana 2, hecho): paradas activas de la ruta del chofer en el día (hoy en CDMX si no se manda). El chofer es el operador ligado al usuario; la ruta es la `RutaDia` que oficina le asignó ese día o, si no hay, la de su ruta habitual (salvo que oficina haya dado ese día a otro operador). Cada parada trae orden, tipo, turno, hora específica, prioridad, invitado, teléfono, dirección, referencias, zona, coordenadas, prendas, líneas de servicio y notas; las paradas fijas traen su nombre (y el del lugar), qué se hace y, si son parte de una vuelta, "Traspaso K · paso 2 de 3". Orden provisional hasta la optimización: las fijadas como primera, luego mañana, tarde y cualquier hora (el fin de la mañana sale de `Turno.Manana`), las fijadas como última al final, y los pasos de una vuelta seguidos y en orden. Sin operador, sin ruta o en día inhábil responde con un aviso en `notice`. Permiso: App del chofer.
- `POST /chofer/paradas/{id}/en-camino` (semana 3, hecho): solo en la ruta de hoy, en el orden asignado si `Visita.OrdenObligatorio` está activo y una a la vez. La app pregunta Google Maps o Waze y abre la navegación. El aviso al invitado llega con las notificaciones (semana 6).
- `POST /chofer/paradas/{id}/llegue` (semana 3, hecho): hora real y ubicación del teléfono. Más lejos que `Gps.ToleranciaMetros` responde 409 y la app pide confirmar; confirmada, queda marcada "fuera de radio" para oficina (en Rutas). Sin permiso de ubicación o sin coordenadas de la parada se registra sin distancia. Se puede marcar llegada sin haber marcado en camino. Mientras va en camino, la app mide la distancia (solo con la app abierta) y resalta "Llegué" al entrar al radio.
- `GET /chofer/ruta` trae además la hora estimada de cada parada (la misma de Rutas), la siguiente parada, si el orden es obligatorio y la tolerancia.
- **Cerrar la visita** (semana 4, hecho). Botón "Cerrar visita" en la parada en camino o en sitio (o directo desde una pendiente que ya le toca). Pantalla con "Se realizó" o "No se realizó":
    - `POST /chofer/evidencias` (multipart): foto con la cámara, comprimida en el teléfono (calidad 50 %), con ubicación y distancia a la parada. JPG, PNG o WebP hasta 8 MB; el API revisa que de verdad sea imagen. Se guarda en disco (`Archivos:Ruta`).
    - `POST /chofer/evidencias/firma`: la firma con el dedo en la entrega; la app manda solo los trazos y el API dibuja un SVG.
    - `POST /chofer/paradas/{id}/cerrar`: recolección con prendas (obligatorias si es de un pedido) → pedido Recolectado y sus prendas actualizadas; entrega con firma y forma y monto de pago de referencia → pedido Entregado; traslado, convenio o insumos solo confirma. Notas opcionales.
    - `POST /chofer/paradas/{id}/no-realizada`: motivo de catálogo (`GET /chofer/motivos`) o escrito, y foto. Entrega → intento fallido y reprogramada; recolección → reprogramada; parada fija → solo queda no realizada.
    - Foto obligatoria según `Visita.FotoObligatoria`, en toda visita. Cada acción lleva `localId`: si la app reenvía (sin señal), el API responde lo mismo sin duplicar.
    - En oficina, la ficha del pedido muestra cuándo se cerró, prendas, notas o motivo, pago y las fotos y la firma (`GET /evidencias/{id}/archivo`).
- `POST /chofer/ubicacion`, `POST /chofer/sincronizar` (lote de la cola offline).
- `POST /chofer/fallas`, `POST /chofer/solicitudes-cambio`.

## Criterios de aceptación

- Una visita cerrada sin señal aparece en oficina en cuanto vuelve la conexión, sin duplicarse.
- "En camino" envía el aviso al invitado una sola vez.
- La ubicación deja de enviarse al cerrar el turno.
