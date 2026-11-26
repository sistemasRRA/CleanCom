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
| `Gps.ToleranciaMetros` | 300 | Validar llegada y evidencia; también descarta lecturas de GPS menos precisas |
| `Gps.HistorialSegundos` | 120 | Cada cuánto se guarda la ubicación del chofer en el recorrido |
| `Gps.HistorialMetros` | 100 | Distancia que guarda la ubicación antes de tiempo |
| `Unidad.ServicioCadaKm` | 10000 | Kilómetros entre servicios, si la unidad no tiene los suyos |
| `Unidad.ServicioCadaMeses` | 6 | Meses entre servicios, si la unidad no tiene los suyos |
| `Parada.ToleranciaLlegadaMinutos` | 15 | Llegada a tiempo (± minutos de la hora estimada) y aviso de atraso en el Panel del día |
| `Gps.SinSenalMinutos` | 10 | Minutos sin ubicación de un chofer en ruta para avisar "sin GPS" |
| `Visita.FotoObligatoria` | Sí | |
| `Visita.OrdenObligatorio` | Sí | El chofer registra las paradas en el orden asignado |
| `Fijas.DiasAnticipacion` | 7 | Generación automática |
| `Crm.DiasNuevo` | 45 | |
| `Crm.DiasInactivo` | 120 | También define recuperado y perdido |
| `Crm.FactorEnRiesgo` | 2 | Veces su frecuencia |
| `Crm.DiasNoContesto` | 1 | Volver a llamar si no contestó (registro de contacto sin fecha) |
| `Crm.DiasLlamarDespues` | 3 | Volver a llamar si pidió que le llamaran después |
| `Crm.DiasNoPorAhora` | 21 | Volver a llamar si dijo que no por ahora |
| `Pedido.DiasPermitidosPorEtapa` | Por etapa | Semáforo de atraso |
| `Base.Direccion` | Calz. de los Leones 165, Los Alpes, Álvaro Obregón, 01710 | Inicio y fin de ruta |
| `Base.Ubicacion` | 19.3608092,-99.1968494 | Coordenadas de la base |
| `Ruta.VelocidadPromedio` | 18 | km/h para estimar traslados mientras no se conecta Google |
| `Ruta.FactorDistancia` | 1.4 | Distancia en calle respecto a la línea recta, mientras no se conecta Google |
| `Parada.VentanaHoraEspecifica` | 30 | Minutos antes y después de una hora específica |
| `Promociones.MetaCanjeSellos` | 40 | Meta de canje de los sellos, en % |
| `Promociones.MetaCanjeCuponera` | 30 | Meta de canje de la cuponera, en % |
| `Promociones.TopeSello` | 30 | % máximo de los sellos otorgados que puede tener un solo sello antes de avisar |

Notas de implementación:

- Cada cambio es un registro nuevo con `VigenteDesde`; aplica el más reciente que ya empezó. Un cambio programado a futuro no aplica hasta su fecha, y no se aceptan fechas pasadas.
- Si la ruta tiene valor propio en una clave (R), gana el de la ruta.
- Formatos: hora `HH:mm`, rango `HH:mm-HH:mm`, coordenada `latitud,longitud`, sí/no como `true`/`false`.
- `Pedido.DiasPermitidosPorEtapa` todavía no tiene clave: falta confirmar los días por etapa (ver pendientes).

## Zonas

1. Cada zona pertenece a una ruta. Las zonas EXT (por ejemplo K1EXT) siguen los días de visita de su zona núcleo.
2. **Días de visita (máster):** zona 1 lunes y jueves, zona 2 martes y viernes, zona 3 miércoles y sábado. El sistema actual tiene marcadas todas las zonas de lunes a sábado; se corrige al migrar.
3. **Zona por coordenadas:** la zona de una dirección se decide porque sus coordenadas caen dentro del polígono de la zona. Los polígonos se dibujan a partir de la cobertura actual de 156 códigos postales. Mientras una zona no tenga polígono dibujado, se usa su código postal (tabla de cobertura); si el CP está repartido entre zonas, decide la colonia. Una dirección que no cae en ninguna zona queda fuera de cobertura y oficina elige la zona a mano.
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

- La **hora de registro** es automática en la app del invitado; en oficina es editable (para capturar una llamada de antes del corte). Desde la app tampoco se puede elegir la zona a mano ni agendar para otro invitado o con una dirección ajena.
- **Hora específica** y **prioridad** solo las puede usar oficina.

### Cambios y cancelaciones desde la app del invitado

| Situación | Regla | Ejemplo |
| --- | --- | --- |
| Cambio de día antes de `CorteCambioDia` | Nueva fecha mínima: día siguiente | Cambio el día 2 a las 11:59 → día 3 |
| Cambio de día a partir de `CorteCambioDia` | Nueva fecha mínima: dos días después | Cambio el día 2 a las 12:01 → día 4 |
| Cancelación | Permitida hasta `CorteCancelacion` (13:00 del día anterior a la recolección); después solo oficina | Recolección el día 5: cancela hasta el día 4 a las 13:00 |

La nueva fecha respeta además días de zona, días inhábiles y cupo.

Solo se cambia o cancela desde la app una **recolección que falta por hacer**: el pedido está en Agendado y el chofer no ha salido hacia ella (su parada sigue pendiente). Con el chofer en camino o en sitio, o con las prendas ya recogidas, la app pide comunicarse con oficina. Al cambiar de día, la parada anterior se da de baja y el historial del pedido guarda "Recolección reprogramada … Antes: …"; al cancelar, el pedido pasa a Cancelado con el motivo "Cancelado por el invitado desde la app" (más lo que escriba) y su parada sale de la ruta, igual que cuando cancela oficina.

## Rutas y optimización

1. Cada ruta sale de la base a `SalidaBase` y regresa a la base; la comida es una parada de `Comida.Minutos`.
2. Ventanas por turno: mañana 09:00 a 13:00, tarde 13:00 a 18:00, cualquier hora = toda la jornada, hora específica = ventana corta alrededor de esa hora. Las restricciones escritas ("antes de las 4:00 p.m.", "después de cargar en Leones") se capturan como ventana o como paso de viaje.
3. **Viajes:** los pasos de un viaje (por ejemplo Florida → Del Valle → Florida) se visitan en orden. No tienen que ir seguidos: Operadora carga en Leones antes de las 12:00 y entrega en Cuautitlán por la tarde antes de las 14:00. Cada paso tiene su propio qué se hace, turno y hora límite ("Antes: a las 9:30 a.m."), que cierra su ventana.
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
6. La agenda cuenta las fijas también en los días que aún no se generan (más allá de `Fijas.DiasAnticipacion`). Una vez generado un día, cuentan sus paradas como cualquier otra; si oficina quita una de la ruta, no regresa sola.
7. Pausar, desactivar, cambiar o saltar una fecha de una fija reemplaza sus paradas desde hoy que no se hayan iniciado.

## Pedidos

### Etapas

`Agendado → Recolectado → EnPlanta → Listo → EntregaConfirmada → EnCamino → Entregado`, además de `Cancelado`.

| Transición | Disparador automático |
| --- | --- |
| Agendado → Recolectado | El chofer cierra la recolección en la app |
| Recolectado → EnPlanta | Llega el primer ticket del POS ligado al pedido |
| EnPlanta → Listo | Todos los tickets del pedido marcados como listos |
| Listo → EntregaConfirmada | Oficina programa la entrega con el invitado: día con cupo y turno (mañana, tarde o cualquier hora; la entrega no lleva hora específica). Sin elegir turno se conserva el de la última entrega o, si no hay, el de la recolección. El invitado no elige la fecha de entrega: depende de la operación y la demanda |
| EntregaConfirmada → EnCamino | El chofer marca "en camino" a esa entrega |
| EnCamino → Entregado | El chofer cierra la entrega con evidencia |

- Mover una tarjeta a mano está permitido; regresarla o entregarla a mano pide motivo y queda en el historial.
- **Cerrar la visita (app del chofer):** cerrar una recolección pide las prendas y pasa el pedido a Recolectado; cerrar una entrega pasa el pedido a Entregado (firma y pago de referencia opcionales). `Visita.FotoObligatoria` aplica a toda visita, realizada o no. Una visita se puede cerrar sin haber marcado en camino o llegué, con la misma regla de orden.
- **Recolección no realizada:** el chofer registra motivo y foto; la recolección se reprograma al siguiente día con cupo de su zona desde mañana y queda en el historial. No cuenta como intento fallido de entrega.
- **No se entregó:** el chofer registra motivo y foto. El pedido se reprograma a la siguiente fecha disponible de su zona y se avisa a oficina y al invitado. **Sin límite de intentos**; cada intento queda en el historial y el pedido aparece en el filtro de intentos fallidos.
- **Atraso:** un pedido está atrasado si pasa los días permitidos de su etapa o si su fecha de entrega ya pasó. La tarjeta muestra semáforo antes de vencerse.
- **Entrega parcial:** si hay tickets listos y otros no, oficina decide entregar lo listo o esperar.
- La columna Entregado del tablero muestra los últimos 7 días.

## Planta

- Los tickets llegan del POS con folio, línea de servicio y prendas. Mientras se define la conexión con el POS, planta los captura a mano en Tickets en planta. El folio es único entre tickets activos.
- El primer ticket pasa el pedido a En planta; un pedido Listo o más adelante no recibe tickets hasta regresarlo a En planta con motivo.
- Un ticket capturado por error se da de baja con motivo, solo mientras está en proceso.
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

Si no se indica fecha para volver a contactar: mañana si no contestó, en 3 días si pidió que le llamaran, en 3 semanas si dijo que no por ahora (días configurables: `Crm.DiasNoContesto`, `Crm.DiasLlamarDespues`, `Crm.DiasNoPorAhora`). "No contactar" marca al invitado: sale de "Llamar hoy" y de campañas. "Agendó" no programa seguimiento.

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

Cliente final (servicio a domicilio), Convenio, Empresa y Tienda / uso interno, como en la captura de clientes del sistema actual. Las tiendas no cuentan en las métricas del CRM; convenio y empresa sí. Tienda interna no se ofrece al capturar un invitado nuevo.

Al capturar también se guarda **cómo nos conoció**, elegido del catálogo "Cómo nos conoció" (Operación › Catálogos › Motivos, editable; arranca con las 9 opciones del sistema actual), y si **recibe avisos por WhatsApp** (sí por omisión). Los avisos llegan con las notificaciones (semana 6) y solo se mandan a quien lo acepta; es distinto de "No contactar" del CRM, que lo saca de llamadas y campañas.

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

Cómo funciona (semana 6):

1. **Promoción con reglas estructuradas:** programa, línea del descuento, tipo (porcentaje, monto o artículo gratis), valor, mínimo del ticket, forma de pago, acumulable, condición en texto y vigencia en días. El descuento lo aplica el POS; el sistema entrega, aparta y marca los beneficios.
2. **Cuponera al primer servicio:** al cerrarse la recolección del primer servicio de un invitado (el chofer la cierra, o oficina pasa el pedido de Agendado a otra etapa), recibe un cupón de cada promoción de cuponera activa. Cada cupón es de una línea.
3. **Sellos con condición:** del segundo servicio en adelante, al cerrarse la recolección recibe cada sello activo cuya línea "se da al cerrar un servicio de" esté en el pedido (o sin línea: cualquier servicio). No se repite mientras tenga uno de esa promoción sin usar y vigente. Oficina también puede dar un sello o cupón a mano desde la ficha del invitado.
4. **Código único** del tipo `CC-7KQ4-M2XP` (sin letras que se confundan); nunca se reutiliza.
5. **Vigencia:** se puede usar hasta el día de entrega más los días de la promoción. Ejemplo: entregado el 24 nov con 60 días → hasta el 23 ene. Un sello puede valer, en cambio, **hasta la entrega del pedido** que lo dio (los de la demo: "entregar un edredón al recibir este pedido"): vence el día de entrega de ese pedido y se mueve si oficina cambia la entrega; mientras la entrega no esté programada no tiene fecha. Dado a mano, va con el pedido en proceso del invitado (si no tiene uno, no se puede dar).
6. **Apartar:** al pedir el servicio (app o Agendar) se eligen beneficios disponibles; deben vencer en o después del día de recolección, ser de una línea del pedido y, si son varios, todos acumulables. Quedan apartados para ese pedido.
7. **Usar:** quedan usados al cerrarse la recolección del pedido. Oficina puede marcar uno como usado con su código; uno usado o vencido no se vuelve a usar.
8. **Liberar:** si el pedido se cancela o se da de baja, sus beneficios apartados regresan al invitado.
9. **Tiendas internas** no reciben beneficios.
10. **Tope de un sello:** si un sello pasa de `Promociones.TopeSello` de los sellos otorgados, Promociones lo marca y al darlo a mano se avisa; se sigue pudiendo dar.

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

Cómo funciona (semana 6, modo de prueba):

1. **Eventos:** recolección agendada (oficina o app), cambio de día (el invitado, oficina, o el chofer no la hizo), pedido cancelado, chofer en camino (recolección), recolección realizada (el chofer la cierra, u oficina pasa el pedido de Agendado a otra etapa), prendas en proceso (llega a En planta; inactiva), entrega confirmada (al programar o mover la entrega, con fecha y turno), entrega en camino y entregado. La encuesta NPS llega con las encuestas.
2. **Canal:** push si el invitado tiene la app registrada en algún dispositivo; si no, WhatsApp de utilidad solo si acepta avisos por WhatsApp. Sin ninguno, el aviso queda registrado como "No se avisó" con el motivo. Las tiendas internas no reciben avisos.
3. **Plantilla por evento** (WhatsApp y metas): nombre en Meta, texto de WhatsApp con variables `{{1}}`, `{{2}}`… y título y texto del push. Una plantilla inactiva no avisa su evento. Variables: nombre (el primer nombre), fecha ("jueves 26 de noviembre"), turno ("en la mañana"), número de pedido o prendas, según el evento.
4. **Bandeja de salida:** el aviso se registra con el texto ya armado en la misma operación que lo dispara y un proceso lo envía cada 10 segundos; un proveedor lento nunca frena al chofer ni a oficina.
5. **Repetidos:** el mismo aviso del mismo pedido con los mismos datos dentro de 10 minutos no se repite (doble clic, reenvío). Una fecha distinta sí avisa.
6. **Modo de prueba** (sin cuenta de Meta ni proyecto de Expo): el WhatsApp queda "Por mandar a mano" con el botón "Abrir WhatsApp" (texto escrito) y "Ya lo mandé"; el push queda "Simulado". Al conectar Meta o Expo cambia solo el remitente. Si un envío real de WhatsApp falla, también queda para mandar a mano.
7. **Avisos a oficina (campana):** cuando el invitado pide un servicio, cambia el día o cancela desde la app, aparece en la campana del menú para quien ve Agendar, Pedidos o Clientes (CRM); cualquiera lo marca como atendido para todos.

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

Cómo se calcula (semana 7, en el API):

1. **Periodos.** La semana va de lunes a domingo; el mes y el trimestre son de calendario. Las visitas cuentan por el día de su ruta; tickets, avisos y contactos por cuándo se registraron (hora de Ciudad de México).
2. **Cada indicador**, con lo que se muestra en su detalle:
   - *Recolecciones por ruta al día:* recolecciones realizadas / días en que cada ruta cerró alguna visita. Ejemplo: F hizo 18 en 2 días y K 6 en 1 día → 24 / 3 = 8.
   - *Prendas por ticket y facturación sin convenios:* tickets de clientes finales (sin convenio, empresa ni tienda interna). *Ticket promedio:* todos los tickets con importe. Mientras no está el POS, salen de los tickets capturados en Tickets en planta.
   - *Cobro anticipado:* pedidos entregados en el periodo con un cobro de referencia registrado en su recolección / pedidos entregados.
   - *Clientes nuevos:* invitados cuyo primer servicio (recolección de un pedido no cancelado, la misma fecha que usa el CRM) cayó en el periodo.
   - *Clientes atendidos:* invitados distintos con una visita realizada. *Recurrencia:* servicios / invitados distintos que los tuvieron.
   - *Tickets firmados:* entregas realizadas con firma / entregas realizadas. *Visitas con foto:* visitas realizadas con foto / visitas realizadas (sin contar salida, regreso ni comida).
   - *Pedidos entregados fuera de tiempo:* entregados después de la primera fecha de entrega que se les programó (reprogramar no la mueve).
   - *Llegadas a tiempo:* cada "Llegué" contra la hora estimada del plan de ese día, con ± `Parada.ToleranciaLlegadaMinutos` (igual que el Panel del día).
   - *Toques por WhatsApp:* avisos de WhatsApp enviados o mandados a mano, más contactos del CRM por WhatsApp.
   - *Servicios fuera de cobertura:* visitas realizadas a direcciones fuera de todo polígono de zona activo.
   - *NPS y encuestas:* sin datos hasta que la app del invitado mande la encuesta.
3. **Por ruta:** el mismo cálculo con las visitas, tickets o pedidos de cada ruta (la de la zona de la dirección del pedido). Un contacto del CRM no tiene ruta: cuenta solo en el total.
4. **Semáforo:** súper verde si alcanza la súper verde, verde si alcanza la verde y rojo si no la alcanza ("alcanzar" es ≥ cuando más es mejor y ≤ cuando menos es mejor). Sin valor o sin meta: "sin meta". Verde es obligatorio; rojo y súper verde son opcionales (sin súper verde nunca se pinta súper verde). El valor rojo se guarda y se muestra como referencia (ver pendientes). Ejemplo: tickets firmados con solo verde 100 → 100 % es verde y 97 % es rojo.
5. **Metas con vigencia:** una meta aplica desde la fecha que se indica (hoy o después). El semáforo de un periodo usa la meta vigente en su último día (o hoy, si sigue abierto). Ejemplo: verde 8 desde el 1 de noviembre y verde 10 desde el miércoles 25 → la semana del 16 al 22 se pinta con 8 y la del 23 al 29 con 10. Una meta que todavía no empieza se puede quitar; una vigente se reemplaza con otra.
6. **Orden de la meta:** más es mejor → rojo < verde < súper verde; menos es mejor → rojo > verde > súper verde (solo entre los valores capturados).
   **Metas por ruta:** una meta es de toda la empresa o de una ruta. Cada ruta del desglose se pinta con su meta; sin meta propia usa la de la empresa solo en promedios y porcentajes (la meta de un conteo es de toda la empresa). La empresa usa su meta; sin ella, si todas las rutas activas tienen meta, la suma de las de ruta en conteos y montos (clientes nuevos 6 en F y 6 en K → 12) o su promedio en promedios y porcentajes. Ejemplo: facturación F verde 16,963, K 12,650 y empresa 29,613 a la semana: cada una con la suya.
7. **Meta y periodo.** La meta se escribe al día, a la semana o al mes:
   - Promedios y porcentajes: valen igual para cualquier periodo.
   - Conteos y montos que se suman (clientes nuevos, facturación, toques, fuera de tiempo, fuera de cobertura): se escalan por días; al mes cuenta 1 por mes completo (3 en un trimestre) y días / 30 en otro caso. Ejemplo: 20 clientes nuevos a la semana en un mes de 31 días → 88.57.
   - Invitados distintos (clientes atendidos): no se suman de una semana a otra, así que la meta solo se compara en periodos de su tipo (semanal con semanas, mensual con meses); en otro periodo la tarjeta dice por qué no tiene semáforo.
   - En el periodo abierto, los conteos se comparan con la parte transcurrida. Ejemplo: 35 clientes atendidos a la semana, el miércoles → 15.
   - El desglose por ruta usa la meta solo en promedios y porcentajes: la meta de un conteo es de toda la empresa.
8. **Cálculo guardado:** cada noche a la 1:00 se calculan y guardan la semana, el mes y el trimestre en curso y los anteriores. Un periodo que nadie ha calculado se calcula la primera vez que se ve; "Recalcular" (KPIs, Editar) lo hace al momento. Un periodo futuro no se guarda.

## Panel del día

1. **Estado de cada ruta:** "sin salir" mientras ninguna parada empezó; "en ruta" con alguna en camino, en sitio o cerrada y otras por visitar; "de regreso" cuando todas están cerradas. Salida, regreso y comida no cuentan como visitas.
2. **Llegada a tiempo:** la hora real de "Llegué" está a ± `Parada.ToleranciaLlegadaMinutos` de la hora estimada del plan. El indicador es llegadas a tiempo / llegadas del día. Ejemplo: estimada 10:30, cuentan de 10:15 a 10:45.
3. **Atraso (solo hoy):** la siguiente parada por visitar tenía hora estimada hace más de `Parada.ToleranciaLlegadaMinutos`. Ejemplo: estimada 10:30, a las 10:46 la tarjeta avisa "Atraso de 16 min".
4. **Horas extra:** el regreso estimado pasa de `Ruta.RegresoMaximo` (o el de sábado).
5. **Sin GPS (solo hoy):** una ruta "en ruta" cuyo chofer no ha mandado ubicación en `Gps.SinSenalMinutos`, o nunca.
6. **Pendientes (siempre al día de hoy, sin importar el día en pantalla):** paradas de días pasados sin cerrar; pedidos en Listo sin fecha de entrega; pedidos con `Planta.DiasProceso` días o más en planta (contados desde que entraron a esa etapa). Cada contador es el tamaño de la lista que abre.
7. **Indicadores del día:** recolecciones y entregas realizadas contra planeadas, cobrado de referencia (lo que declaran los choferes), % de llegadas a tiempo y pedidos en planta ahora (en planta o listos).
8. **Carga de 7 días** desde hoy: paradas que ocupan cupo, con las fijas, contra el cupo de cada ruta (lo mismo que cuenta la agenda).

## GPS del chofer

1. Durante el turno, la app manda la ubicación del teléfono cada 30 a 60 segundos. Sin señal las guarda y las manda juntas al volver la conexión.
2. Solo se guarda si el chofer tiene ruta ese día. Se descartan las lecturas que no son del día de la ruta, las que tienen hora futura y las que tienen precisión peor que `Gps.ToleranciaMetros`.
3. **Mapa en vivo:** la lectura más reciente es la posición del chofer y se envía a oficina en ese momento (SignalR). Una lectura atrasada (por ejemplo, de cuando no había señal) no regresa el punto del mapa. Si pasan `Gps.SinSenalMinutos` sin una lectura nueva, el punto se queda donde estaba pero se ve gris ("sin señal desde las 11:00"), con cualquier estado de la ruta; el API manda con cada posición desde cuándo deja de ser actual (`signalLostAt`). Ejemplo: última lectura 11:00 con 10 minutos → gris desde las 11:10.
4. **Recorrido (historial reducido):** se guarda la primera lectura del día y después una cada `Gps.HistorialSegundos`, o antes si el chofer se movió `Gps.HistorialMetros` o más. Un reenvío no se guarda dos veces.

Ejemplo: con lecturas cada 30 s sin moverse, se guardan las de 10:02 y 10:04 después de la de 10:00. Si a las 10:01 ya avanzó 150 m, esa también se guarda.

5. **El invitado ve a su chofer** solo mientras su parada está "en camino": antes (aunque el chofer ya esté en ruta) y después de "Llegué" no ve su ubicación. Ve su nombre de pila, la ruta, la última posición (gris pasados `Gps.SinSenalMinutos`) y la hora estimada de llegada: desde la hora de esa posición, la línea recta a su dirección por `Ruta.FactorDistancia` a `Ruta.VelocidadPromedio`, el mismo cálculo del plan de ruta mientras no se conecta Google. Ejemplo: a unos 3 km en línea recta, × 1.4 = 4.2 km a 18 km/h → 14 minutos después de la lectura.

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

Cómo se calcula (semana 5, en el API):

1. **Días de aviso editables.** Los días de cada evento, sus destinatarios y canales están en las reglas de aviso (`GET|PUT /avisos-flota/reglas`). Valores iniciales: verificación 15 y 0, seguro 30, 7 y 0, pagos 15 y 3, tarjeta 30, servicio 15 y 0, falla 0.
2. **Verificación por placa (calendario de la CDMX).** Una unidad marcada "exenta de verificación" (las trimotos) no tiene calendario ni aviso. Para las demás cuenta el último dígito de la placa: 5 y 6 verifican en enero-febrero y julio-agosto; 7 y 8 en febrero-marzo y agosto-septiembre; 3 y 4 en marzo-abril y septiembre-octubre; 1 y 2 en abril-mayo y octubre-noviembre; 9 y 0 en mayo-junio y noviembre-diciembre. Si no hay verificación registrada desde que empezó el periodo vigente, "toca verificar" hasta que cierra y después queda vencida. Ejemplo: placa que termina en 4, última verificación en marzo; el 20 de noviembre está vencida porque el periodo de septiembre-octubre cerró sin verificar.
3. **Seguro y tarjeta de circulación** por su fecha de vigencia. Ejemplo: seguro que vence el 20 de diciembre avisa el 20 de noviembre (30 días), el 13 de diciembre (7) y el 20 de diciembre.
4. **Servicio por km o fecha, lo que llegue primero.** El siguiente servicio es el último más `ServicioCadaKm` y más `ServicioCadaMeses` (de la unidad o de la configuración). Ejemplo: servicio el 1 de mayo a 20,000 km, cada 10,000 km o 6 meses → toca el 1 de noviembre o a los 30,000 km. Una unidad sin servicio registrado se marca.
5. **Pagos recurrentes** por su próxima fecha. Registrar el pago en la bitácora mueve la fecha según su periodicidad (mensual, bimestral, trimestral, semestral o anual) y suma al gasto del año.
6. **Recordatorio:** pasada la fecha, el aviso se repite cada 7 días hasta que se registra algo. El servicio vencido por kilometraje (sin fecha) se recuerda los lunes.
7. **Gasto del año y costo por km:** suma de los montos de la bitácora del año, entre los kilómetros recorridos en el año (del primer kilometraje registrado del año al actual).
8. **Asignar una unidad a una ruta** fuera de servicio, con falla abierta o con seguro, tarjeta o verificación vencidos pide confirmar. Una unidad no puede estar en dos rutas el mismo día.
