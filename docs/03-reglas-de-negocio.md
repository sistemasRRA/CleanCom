# Reglas de negocio

Fuente única de las reglas del sistema. Todas se implementan en el API y tienen pruebas unitarias con los ejemplos de este documento. Toda hora se evalúa en hora de Ciudad de México (`America/Mexico_City`).

## Configuración

Valores iniciales, editables en la pantalla Operación. Cada cambio queda en historial y puede programarse a partir de una fecha. Los marcados con (R) pueden sobrescribirse por ruta.

| Clave | Valor inicial | Uso |
| --- | --- | --- |
| `Agenda.CorteMismoDia` | 13:00 | Después ya no entra el mismo día |
| `Agenda.CorteCambioDia` | 12:00 | Cambio de día desde la app del invitado |
| `Agenda.CorteCancelacion` | 13:00 del día anterior | Cancelación desde la app del invitado |
| `Ruta.SalidaBase` | 09:00 | Salida de Leones |
| `Ruta.RegresoMaximo` (R) | 18:00 | Fin de ruta y alerta de horas extra |
| `Ruta.RegresoMaximoSabado` (R) | 14:00 | |
| `Turno.Manana` | 09:00 a 13:00 | |
| `Turno.Tarde` | 13:00 a 18:00 | |
| `Comida.Minutos` / `Comida.Desde` | 60 / 13:00 | Pausa dentro de la ruta |
| `Ruta.MetaRecolecciones` (R) | 10 | KPI y panel |
| `Ruta.CupoParadas` (R) | 14 | 10 recolecciones + 4 entregas; las fijas cuentan |
| `Unidad.CapacidadPrendas` | 140 | Aviso en planeación |
| `Planta.DiasProceso` | 3 | Por línea de servicio en `LineaServicio.DiasProceso` |
| `Parada.MinutosRecoleccion` | 15 | Horas estimadas |
| `Parada.MinutosEntrega` | 10 | |
| `Parada.MinutosTraslado` | 12 | Traslado o insumos |
| `Gps.ToleranciaMetros` | 300 | Validar llegada y evidencia |
| `Visita.FotoObligatoria` | Sí | |
| `Visita.OrdenObligatorio` | Sí | El chofer registra las paradas en el orden asignado |
| `Fijas.DiasAnticipacion` | 7 | Generación automática |
| `Crm.DiasNuevo` | 45 | |
| `Crm.DiasInactivo` | 120 | También define recuperado y perdido |
| `Crm.FactorEnRiesgo` | 2 | Veces su frecuencia |
| `Pedido.DiasPermitidosPorEtapa` | Por etapa | Semáforo de atraso |
| `Base.Direccion` | Calz. de los Leones 165, Los Alpes, Álvaro Obregón, 01710 | Inicio y fin de ruta |
| `Base.Ubicacion` | 19.3608092,-99.1968494 | Coordenadas de la base |

Notas de implementación:

- Cada cambio es un registro nuevo con `VigenteDesde`; aplica el más reciente que ya empezó. Un cambio programado a futuro no aplica hasta su fecha, y no se aceptan fechas pasadas.
- Si la ruta tiene valor propio en una clave (R), gana el de la ruta.
- Formatos: hora `HH:mm`, rango `HH:mm-HH:mm`, coordenada `latitud,longitud`, sí/no como `true`/`false`.
- `Pedido.DiasPermitidosPorEtapa` todavía no tiene clave: falta confirmar los días por etapa (ver pendientes).

## Zonas

1. Cada zona pertenece a una ruta. Las zonas EXT (por ejemplo K1EXT) siguen los días de visita de su zona núcleo.
2. **Días de visita (máster):** zona 1 lunes y jueves, zona 2 martes y viernes, zona 3 miércoles y sábado. El sistema actual tiene marcadas todas las zonas de lunes a sábado; se corrige al migrar.
3. **Zona por coordenadas:** la zona de una dirección se decide porque sus coordenadas caen dentro del polígono de la zona. Los polígonos se dibujan a partir de la cobertura actual de 156 códigos postales.
4. **Fuera de cobertura o en el borde:** si la dirección no cae en ningún polígono o cae en el borde de dos, el sistema propone la ruta más factible: la que suma menos tiempo y kilómetros a las paradas que ya tiene ese día. Oficina puede corregir la zona a mano (`ZonaAsignadaManual`). Estos servicios cuentan en el KPI "servicios fuera de cobertura".

## Agenda (motor único)

Usado por la web (Agendar) y la app del invitado. Dos operaciones:

### `disponibilidad(direccion, turno, horaRegistro)`

1. Obtener la zona por coordenadas y de ahí la ruta y sus días de visita.
2. Fecha mínima: si `horaRegistro` es antes de `CorteMismoDia`, hoy; si no, mañana.
3. Recorrer los días desde la fecha mínima y devolver el primero que: sea día de visita de la zona, no sea día inhábil y tenga cupo (`paradas que ocupan cupo < CupoParadas`), contando las paradas fijas de ese día.
4. Devolver también los siguientes días de la zona con su ocupación (por ejemplo "mar 3 nov 8/14") y la explicación en texto de cómo se calculó.

Ejemplo (sistema actual): zona K3, registro a las 18:51 después del corte de las 13:00 → no entra el mismo día → primer día con cupo: martes 3 de noviembre, lugar 9 de 14.

### `agendar(...)`

1. Repetir el cálculo dentro de una transacción con bloqueo por (ruta, fecha).
2. Si el cupo ya no alcanza, responder con la siguiente fecha disponible, sin crear nada.
3. Crear el pedido en etapa Agendado y la parada de recolección en la `RutaDia`.
4. Disparar el aviso "cita agendada".

- La **hora de registro** es automática en la app del invitado; en oficina es editable (para capturar una llamada de antes del corte).
- **Hora específica** y **prioridad** solo las puede usar oficina.

### Cambios y cancelaciones desde la app del invitado

| Situación | Regla | Ejemplo |
| --- | --- | --- |
| Cambio de día antes de `CorteCambioDia` | Nueva fecha mínima: día siguiente | Cambio el día 2 a las 11:59 → día 3 |
| Cambio de día a partir de `CorteCambioDia` | Nueva fecha mínima: dos días después | Cambio el día 2 a las 12:01 → día 4 |
| Cancelación | Permitida hasta `CorteCancelacion` (13:00 del día anterior a la recolección); después solo oficina | Recolección el día 5: cancela hasta el día 4 a las 13:00 |

La nueva fecha respeta además días de zona, días inhábiles y cupo.

## Rutas y optimización

1. Cada ruta sale de la base a `SalidaBase` y regresa a la base; la comida es una parada de `Comida.Minutos`.
2. Ventanas por turno: mañana 09:00 a 13:00, tarde 13:00 a 18:00, cualquier hora = toda la jornada, hora específica = ventana corta alrededor de esa hora. Las restricciones escritas ("antes de las 4:00 p.m.", "después de cargar en Leones") se capturan como ventana o como paso de viaje.
3. **Viajes:** los pasos de un viaje (por ejemplo Florida → Del Valle → Florida) se visitan en orden y seguidos.
4. **Posición fija:** una parada movida a mano o marcada como primera o última queda fija; el recálculo acomoda las demás.
5. El orden se calcula con Google Route Optimization (una ruta a la vez). Se recalcula solo ante cambios relevantes: parada nueva o cancelada, cambio manual, inicio de turno o retraso del chofer mayor a 15 minutos. Nunca por cada lectura de GPS.
6. **Alertas antes de guardar:** regreso estimado posterior a `RegresoMaximo` (horas extra), parada fuera de su ventana, prendas estimadas mayores a la capacidad de la unidad, unidad con seguro o verificación vencidos o falla abierta.
7. **Solo oficina reordena.** El chofer puede solicitar un cambio desde la app; oficina lo aprueba.

## Paradas fijas

1. Se generan solas cada noche para los próximos `Fijas.DiasAnticipacion` días, según días, ruta, turno y vigencia. No se generan en días inhábiles ni en fechas de excepción.
2. **Cuentan dentro del cupo de 14.** Ejemplo: la Ruta K con el Traspaso K (3 paradas) deja 11 lugares para invitados.
3. Una vuelta fija genera un `Viaje` con sus pasos en orden.
4. Regenerar un día reemplaza solo las paradas fijas de ese día que no se hayan iniciado.
5. Insumos: nota de texto, sin inventario.

## Pedidos

### Etapas

`Agendado → Recolectado → EnPlanta → Listo → EntregaConfirmada → EnCamino → Entregado`, además de `Cancelado`.

| Transición | Disparador automático |
| --- | --- |
| Agendado → Recolectado | El chofer cierra la recolección en la app |
| Recolectado → EnPlanta | Llega el primer ticket del POS ligado al pedido |
| EnPlanta → Listo | Todos los tickets del pedido marcados como listos |
| Listo → EntregaConfirmada | Oficina programa la entrega con el invitado |
| EntregaConfirmada → EnCamino | El chofer marca "en camino" a esa entrega |
| EnCamino → Entregado | El chofer cierra la entrega con evidencia |

- Mover una tarjeta a mano está permitido; regresarla o entregarla a mano pide motivo y queda en el historial.
- **No se entregó:** el chofer registra motivo y foto. El pedido se reprograma a la siguiente fecha disponible de su zona y se avisa a oficina y al invitado. **Sin límite de intentos**; cada intento queda en el historial y el pedido aparece en el filtro de intentos fallidos.
- **Atraso:** un pedido está atrasado si pasa los días permitidos de su etapa o si su fecha de entrega ya pasó. La tarjeta muestra semáforo antes de vencerse.
- **Entrega parcial:** si hay tickets listos y otros no, oficina decide entregar lo listo o esperar.
- La columna Entregado del tablero muestra los últimos 7 días.

## Planta

- Los tickets llegan del POS con folio, línea de servicio y prendas.
- Planta marca cada ticket como listo escaneando o escribiendo el folio.
- El atraso de un ticket se mide contra los días de proceso de su línea de servicio.
- Un comentario de atraso tiene motivo de catálogo opcional y texto libre; aparece en Pedidos.

## Cobro

- El cobro oficial se registra en el POS. En la app el chofer anota forma y monto como referencia.
- El corte de caja compara lo declarado por el chofer contra lo registrado en las visitas y contra el POS, y marca diferencias.

## CRM

### Frecuencia y estatus

- **Frecuencia** de un invitado: mediana de días entre sus servicios. Oficina puede fijar una frecuencia manual.
- **Le toca (fecha):** último servicio + frecuencia. La visita sugerida es el siguiente día de visita de su zona a partir de esa fecha.

| Estatus | Regla |
| --- | --- |
| Prospecto | Sin servicios |
| Nuevo | Primer servicio en los últimos `Crm.DiasNuevo` días |
| Activo | Pide con su frecuencia normal |
| Le toca | Su fecha "le toca" ya llegó o llega antes de su próxima visita de zona |
| En riesgo | Ya tuvo servicio y lleva `Crm.FactorEnRiesgo` veces su frecuencia sin pedir |
| Inactivo | Más de `Crm.DiasInactivo` días sin servicio |

El estatus se recalcula cada noche y guarda el "por qué" en texto ("pide cada 7 días; su último servicio fue hace 9"). Oficina puede fijar un estatus manual.

### Lista "Llamar hoy"

Invitados Le toca o En riesgo cuya ruta pasa mañana por su zona, excluyendo a quien tenga `ProximoContacto` futuro o haya pedido no ser contactado.

### Registro de contacto

Si no se indica fecha para volver a contactar: mañana si no contestó, en 3 días si pidió que le llamaran, en 3 semanas si dijo que no por ahora.

### Clientes y fuentes

| Término | Definición |
| --- | --- |
| Nuevo | Su primer pedido cae en el periodo |
| Recurrente | Su pedido anterior fue hace menos de 120 días |
| Recuperado | Vuelve tras más de 120 días sin pedir |
| Perdido | Cumple 120 días sin pedir dentro del periodo |
| Activo al cierre | Pidió en los 120 días previos al cierre del periodo |

El periodo en curso se marca como parcial y se compara contra los mismos días del periodo anterior.

### Tipos de invitado

Cliente final, Empresa (convenio) y Tienda / uso interno. Las tiendas no cuentan en las métricas del CRM.

## Promociones

| Programa | Para quién | Meta de canje |
| --- | --- | --- |
| Sellos de Promociones Cruzadas | Invitados recurrentes | 40% o más |
| Cuponera de Bienvenida | Invitados nuevos (al cerrar su primer servicio) | 30% o más |
| Campañas | Segmentos del CRM por ruta y estatus | Por campaña |

- Un solo sello no debe pasar del 30% de los otorgados; se avisa antes de seguir dándolo.
- Campañas: no se envían a quien pidió no ser contactado ni a quien ya tiene el beneficio.
- Cada beneficio tiene un código único. El chofer y oficina lo marcan como usado; el descuento se aplica en el POS.
- Cuponera: líneas Tintorería, Lavandería, Planchaduría, Sastrería y Calzado, vigencia de 60 días; un cupón inactivo no se entrega.

## Avisos al invitado

| Evento | Canal |
| --- | --- |
| Recolección agendada | Push si tiene app; si no, WhatsApp de utilidad |
| Cambio o cancelación | Igual |
| Chofer en camino (con hora estimada y liga de seguimiento) | Igual |
| Recolección realizada | Igual |
| Prendas en proceso (opcional, hoy inactiva) | Igual |
| Entrega confirmada | Igual |
| Entrega en camino y entregado | Igual |
| Encuesta NPS | Al entregar |
| Código de verificación | WhatsApp de autenticación; SMS de respaldo |

El envío manual por WhatsApp Web se conserva como respaldo si el envío automático falla.

## KPIs

Cada indicador tiene periodo (diario, semanal o mensual), responsable, rojo, verde y súper verde, y si más o menos es mejor. Las metas tienen vigencia: un cambio no reescribe el semáforo de periodos pasados. Los valores se calculan por proceso programado y se guardan por periodo y ruta.

| Área | Indicador | Cálculo |
| --- | --- | --- |
| Logística | Recolecciones por ruta al día | Recolecciones realizadas / días operados / rutas |
| Logística | Prendas por ticket (fuera de convenio) | Prendas / tickets del POS sin convenio |
| Logística | Cobro anticipado | Pedidos pagados antes de la entrega / pedidos |
| Logística | Clientes nuevos por ruta | Según definición de Nuevo |
| Logística | Clientes atendidos por ruta | Invitados distintos con visita realizada |
| Logística | Tickets firmados | Entregas con firma / entregas |
| Logística | Visitas con foto de evidencia | Visitas con foto / visitas |
| Logística | Pedidos entregados fuera de tiempo | Entregados después de su fecha de entrega |
| Logística | Llegadas a tiempo | Llegadas dentro de ±15 min de la hora estimada / llegadas |
| Logística | NPS | % promotores (9 a 10) − % detractores (0 a 6) |
| Logística | Encuestas respondidas / enviadas | |
| Logística | Toques a clientes por WhatsApp | Mensajes y contactos registrados |
| Logística | Servicios fuera de cobertura | Visitas con dirección fuera de polígono |
| Venta | Ticket promedio | Del POS |
| Venta | Facturación rutas sin convenios (total, F, K) | Del POS |
| Experiencia | Recurrencia | Servicios por invitado en el periodo |

Fuentes externas (HubSpot, pauta y nómina) no se calculan aquí; en fase 2 se cruzan en BigQuery.

## Flota

| Evento | Avisa |
| --- | --- |
| Verificación | Al iniciar su periodo según la placa, 15 días antes de que cierre y al vencer |
| Seguro | 30 y 7 días antes, y el día que vence |
| Pagos recurrentes | 15 y 3 días antes de cada fecha |
| Tarjeta de circulación | 30 días antes |
| Servicio | Por kilometraje o fecha, lo que llegue primero |
| Falla reportada por un chofer | Al momento |

- Avisos por correo y notificación; el aviso se repite hasta que se registra el pago o la renovación con comprobante.
- Resumen semanal por correo de lo que vence en 30 días.
- El kilometraje de la unidad se actualiza con el inicial y final de cada turno.
