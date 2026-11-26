# Pendientes y fase 2

## Por confirmar con CleanCom

- [ ] Sistema POS y cómo llega hoy su información (API, base de datos o archivo). Mientras tanto los tickets se capturan a mano en Tickets en planta.
- [ ] Cerrar visita: formas de pago de referencia (hoy Efectivo, Tarjeta y Transferencia). Confirmar la lista y si hay "ya pagado" o "a crédito".
- [ ] Cerrar visita: los motivos de "no realizada" usan el catálogo de entregas fallidas también para recolecciones. Confirmar si se separan.
- [ ] Cerrar visita: `Visita.FotoObligatoria` pide foto también en traslados, convenios e insumos. Confirmar si solo aplica a invitados.
- [ ] Cerrar visita: la firma del invitado es opcional en la entrega (el KPI "Tickets firmados" mide cuántas la tienen). Confirmar.
- [ ] Cerrar visita: una recolección no realizada se reprograma sola y no cuenta como intento fallido. Confirmar.
- [ ] App del chofer sin señal: un "Llegué" guardado sin señal que al enviarse queda fuera de radio se rechaza (pide confirmar) y se muestra como no guardado; el chofer lo vuelve a marcar. Confirmar si se registra directo como "fuera de radio".
- [ ] Tickets en planta: escanear con la cámara de la tableta (hoy funciona con escáner de código de barras o teclado). Confirmar qué código trae impreso el ticket del POS.
- [ ] Tickets en planta: no hay "deshacer listo"; si se marca por error, se regresa el pedido a En planta en Pedidos. Confirmar si se necesita.
- [ ] Un pedido Listo que recibe otro ticket: hoy hay que regresarlo a En planta a mano primero. Confirmar si debe regresar solo.
- [ ] Base de datos que usa la app actual y respaldo para migrar.
- [ ] Cobertura de cada zona (F1 a K3): qué códigos postales y colonias atiende cada una, para capturarla en Operación › Cobertura. Ya no viene del Excel máster.
- [ ] Dirección de la base (`Base.Direccion`): dice "Los Alpes, 01710", pero en el catálogo oficial de Correos de México Los Alpes es el CP 01010 y el 01710 es Ampliación Alpes o Las Águilas. Confirmar cuál es la correcta.
- [ ] Características y sistema operativo del VPS.
- [ ] Tipos de pagos por unidad (tenencia o refrendo, crédito o arrendamiento, póliza en pagos, otros).
- [ ] Días de proceso prometidos por línea de servicio (planchaduría, lavandería, tintorería, sastrería, calzado).
- [ ] Días permitidos por etapa del pedido antes de marcarlo atrasado.
- [ ] Destinatarios de cada aviso de flota.
- [ ] Textos iniciales del catálogo de motivos (no entrega, atraso en planta, resultado de contacto). El catálogo existe pero arranca vacío. "Cómo nos conoció" ya arranca con las 9 opciones del sistema actual.
- [ ] Tipo de invitado Convenio frente a Empresa: la regla decía "Empresa (convenio)" como uno solo; se separaron como en el sistema actual. Confirmar qué cambia entre ellos (precios, facturación con `Empresa`, métricas) y si un convenio se liga a su empresa.
- [ ] "Recibe avisos por WhatsApp": confirmar si apagarlo también evita los avisos operativos (en camino, listo) o solo los promocionales.
- [ ] Permisos iniciales de Administración (todo menos Roles y permisos), Gerencia (ver todo menos Usuarios y Operación), Logística (rutas, paradas, pedidos, panel y unidades), Telemarketing (agendar, CRM y WhatsApp; ver promociones y pedidos) y Planta (tickets en planta y ver pedidos). Son una propuesta; se ajustan en Usuarios › Roles y permisos.
- [ ] Equipo de los choferes: tableta de la empresa, celular (propio o de la empresa) o ambos; Android, iPhone o ambos. La app es responsiva, pero cambia cómo se distribuye.
- [ ] Distribución de la app del chofer (no se publica en búsquedas de las tiendas). Propuesta: Android con APK interno o prueba interna de Google Play; iOS con Ad Hoc mientras sean menos de 100 equipos y después como app no listada en el App Store. Cambios de JavaScript con EAS Update.
- [ ] App del invitado, chofer en camino: el botón "llamar o escribir" ¿marca al celular del chofer (`Operador.Phone`, dato personal) o a oficina? Si es oficina, falta su número en Configuración. Pospuesto por CleanCom (24 nov 2026): por ahora el seguimiento no muestra teléfonos ni el botón.
- [ ] Programar entrega: la demo tiene "La ropa está lista desde" para mover el primer día que se ofrece. Hoy se ofrecen días desde hoy y la entrega se programa cuando el pedido está Listo (o En planta, parcial). Confirmar si hace falta.
- [ ] Capacidad de prendas por ruta: Operación la pide "por ruta", pero las reglas la marcan como general (`Unidad.CapacidadPrendas`) y la unidad ya tiene la suya. Por ahora no se sobrescribe por ruta.

## Decisiones de implementación por revisar

- [ ] Restablecer contraseña por link o código (usuarios.md): necesita WhatsApp o correo (semana 6). Mientras, oficina asigna una contraseña temporal.
- [ ] Mapa de la dirección con OpenStreetMap (Nominatim y mosaicos públicos) mientras no estén las llaves de Google. Su política de uso es para poco volumen: en producción poner en `Geocoding:UserAgent` un correo de contacto de CleanCom, y pasar a Google Geocoding (o un servidor propio) cuando lleguen las llaves o crezca el uso, por ejemplo con la app del invitado.
- [ ] Verificación en dos pasos para Administración: no está en la semana 1.
- [ ] Bitácora de cambios: hoy cada tabla guarda quién creó y quién modificó por última vez; falta el historial completo de pedidos, rutas, usuarios y metas.

- [x] Agenda: el aviso "cita agendada" al invitado (semana 6, modo de prueba: WhatsApp por mandar a mano, push simulado).
- [x] App del invitado: al pedir, cambiar día o cancelar, oficina lo ve en la campana del menú (semana 6).
- [ ] Avisos: crear la cuenta de Meta Business (número nuevo, verificación) y pedir la aprobación de las 9 plantillas de utilidad (`cleancom_*`, ver WhatsApp y metas › Plantillas) y la de autenticación del código; tardan días. Mientras, los WhatsApp se mandan a mano.
- [ ] Avisos: crear el proyecto de EAS de la app del invitado para obtener el token de push (`expo-notifications`) y enviarlo a `POST /yo/dispositivos`; mientras, el push queda simulado y todo va por WhatsApp. Pasos: (1) CleanCom crea la cuenta de Expo, un proyecto de Firebase (solo FCM, para Android) y la cuenta de Apple Developer; (2) en la app: `eas init` (projectId), `google-services.json` en `app.json`, llave de cuenta de servicio FCM V1 subida a EAS (secreta, nunca al repositorio) y llave APNs de Apple subida a EAS; (3) remitente real de push en el API (Expo Push API) en lugar de `TestPushSender`; (4) probar con una compilación de desarrollo en un Android real (en Expo Go para Android no funciona el push).
- [ ] Avisos: "chofer en camino" sin hora estimada ni liga de seguimiento: falta la página pública de seguimiento (o liga a la app) y decidir si la hora estimada va en el WhatsApp.
- [ ] Avisos: los textos de las plantillas son una propuesta; CleanCom los revisa en WhatsApp y metas (deben coincidir con lo que apruebe Meta).
- [ ] Avisos pendientes de canal: correo (restablecer contraseña, avisos de flota), SMS de respaldo del código (Twilio), campana de flota y campañas de Promociones.
- [ ] Agenda: la propuesta de "ruta más factible" para direcciones fuera de cobertura llega con la optimización de Google (semana 3); mientras, oficina elige la zona a mano.
- [ ] Folio del pedido: falta definir su formato; por ahora se usa el id.
- [ ] Viajes: la regla decía "en orden y seguidos"; con las vueltas reales (Operadora carga en Leones en la mañana y entrega en Cuautitlán en la tarde) quedó "en orden", cada paso con su turno y hora límite. Confirmar.
- [ ] Lavafan (ruta K, viernes): confirmar dirección completa (la colonia no venía) y hora real; la plantilla decía 4:00.
- [ ] Rutas: horas y km son estimados en línea recta hasta conectar Google (proyecto de Google Cloud con facturación, Routes API y Route Optimization con cuenta de servicio). Ajustar `Ruta.VelocidadPromedio` y `Ruta.FactorDistancia` con lo real mientras tanto.
- [ ] App del chofer en Android: el mapa usa Google Maps y en la app instalada (no en Expo Go) necesita una llave de Maps SDK for Android en `android.config.googleMaps.apiKey` de `app.json`. Llega con las llaves de Google. En iPhone usa Apple Maps y no necesita llave.
- [ ] App del chofer: no hay "deshacer en camino" si lo marcó por error; mientras, oficina puede pedirle que marque Llegué al llegar. Confirmar si se necesita.
- [ ] App del chofer: una parada "en sitio" sin cerrar no bloquea la siguiente; oficina la ve en Rutas. Confirmar si debe bloquear.
- [ ] GPS del chofer: el recorrido guarda una ubicación cada 120 s o cada 100 m (`Gps.HistorialSegundos`, `Gps.HistorialMetros`), y se descartan las lecturas con precisión peor que `Gps.ToleranciaMetros`. Son valores propuestos: confirmar con Logística. La última posición vive en memoria del API; con un solo servidor basta, y si algún día hay varios se necesita un backplane de SignalR.
- [ ] Flota: el calendario de verificación por último dígito de la placa es el de la CDMX (engomados). Las trimotos (7J5BR, 7J6BR) quedaron "exentas de verificación". Confirmar si alguna otra unidad tiene holograma 0/00 o placa de otro estado, que verifican distinto.
- [ ] Flota: las 8 unidades se cargaron del sistema anterior (capturas de pantalla, 20 nov 2026) en la base de desarrollo; falta pasarlas a producción con la migración de datos. NY2221B y NY2222B tenían en el año su número económico (2221, 2222): se dejaron como modelo "Tornado 2221/2222", año 2023; confirmar el año real. Ninguna trae fecha de última verificación ni servicio registrado.
- [ ] Flota: servicio cada 10,000 km o 6 meses (`Unidad.ServicioCadaKm`, `Unidad.ServicioCadaMeses`), aviso de servicio 15 días antes y recordatorio cada 7 días después de la fecha (los lunes si se venció por km). Son valores propuestos: confirmar con Administración.
- [ ] Flota: los avisos ya se calculan (y cuáles tocan hoy), pero el envío por correo, la campana de la web y el resumen semanal llegan con las notificaciones (semana 6). La falla reportada desde la app y el kilometraje de inicio y fin de turno llegan con la app del chofer (semana 7).
- [ ] App del invitado: acceso con código de 6 dígitos, 10 minutos, 5 intentos, reenvío al minuto y 5 por hora (`Acceso.*`). Son valores propuestos. El código llega por WhatsApp con las notificaciones (semana 6; plantilla de Meta categoría Autenticación) y SMS de Twilio de respaldo; mientras, solo funciona en desarrollo (código en el log).
- [ ] App del invitado: sin llaves de Google, la dirección se captura a mano (CP y colonia del catálogo) y el pin sale de "Buscar mi dirección" (OpenStreetMap por el API, 20 búsquedas por minuto por usuario), de "Usar mi ubicación" o de tocar el mapa; el autocompletado de Google llega con las llaves. En Android, la app instalada (no Expo Go) necesita la llave de Maps SDK for Android para mostrar el mapa, igual que la del chofer.
- [ ] App del invitado: un invitado que ya estaba en el CRM entra directo a la app; falta el paso de "confirmar sus datos y direcciones" de la primera vez. Confirmar si se quiere.
- [ ] Panel del día: llegada a tiempo y atraso con ±15 min (`Parada.ToleranciaLlegadaMinutos`) y "sin GPS" a los 10 min (`Gps.SinSenalMinutos`). Son valores propuestos: confirmar con Logística. "3 días o más en planta" usa `Planta.DiasProceso` general, no los días de cada línea de servicio.
- [ ] App del chofer: la ubicación en segundo plano pide permiso "Siempre". Apple y Google revisan ese permiso al publicar: hay que explicar en la ficha de la tienda que solo se usa durante el turno, y probarlo en las tabletas reales (algunos Android cierran apps en segundo plano para ahorrar batería y hay que excluir CleanCom Chofer de esa optimización).
- [ ] Paradas fijas: la generación nocturna corre como servicio dentro del API (00:15) mientras no esté Hangfire; pasa a Hangfire con los demás procesos nocturnos.
- [ ] Paradas fijas: tipo de parada (cliente final, convenio, insumos, traspaso) y qué se hace (recolección, entrega, entrega y recolección, traslado) son fijos, como en el sistema actual, y no usan el catálogo editable "Tipos de parada" de Operación. Confirmar si ese catálogo sigue siendo necesario o se quita.
- [ ] Buscar y ver invitados pide permiso de Agendar; cuando exista la pantalla Clientes (CRM) se abre también a ese permiso.

## Reglas propuestas que conviene validar

- [ ] Promociones: los sellos se dan del segundo servicio en adelante al cerrar la recolección, según la línea "se da al cerrar un servicio de", y no se repiten mientras el invitado tenga uno sin usar. Confirmar la mecánica real de "Promociones Cruzadas" (por ejemplo, si un sello se gana con varias visitas). Los cinco sellos de la demo se capturaron sin línea que los dé ("cualquier servicio"), así que hoy un invitado recurrente recibe los cinco: confirmar qué servicio da cada uno (por ejemplo, ofrecer el de una línea que el pedido no trae). "Pago de contado" quedó en la mecánica en texto, no como forma de pago, porque contado puede ser efectivo o tarjeta: confirmar.
- [ ] Promociones: el primer servicio es el primer pedido del invitado que pasa de Agendado (recolectado); la cuponera da un cupón de cada promoción activa. Confirmar si un invitado que ya era cliente antes del sistema (migración) debe recibir cuponera.
- [ ] Promociones: un beneficio apartado queda usado al cerrar la recolección aunque el pedido luego cambie de fecha y pase su vencimiento. El mínimo del ticket y la forma de pago son informativos: los valida el POS. Confirmar.
- [ ] Promociones: metas de canje 40% y 30% y tope de 30% por sello (`Promociones.*`). Ya están capturados los cinco sellos de la demo; falta que CleanCom dé el beneficio impreso de cada cupón de la cuponera para activarlos.

- [ ] Orden de prioridad entre estatus del CRM cuando aplican varios: Inactivo, En riesgo, Le toca, Nuevo y Activo (Prospecto si no hay servicios). Así un invitado nuevo con fecha "le toca" aparece como Le toca.
- [x] KPIs: metas de la hoja "KPI's 2026 CleanCom" (1Q 2026) cargadas desde el 1 de enero de 2026, con metas por ruta para facturación, clientes nuevos y clientes atendidos.
- [ ] KPIs: asignar responsables a las metas (la hoja dice Dirección, Daniela y Gustavo; hay que darlos de alta como usuarios de oficina). Pedidos fuera de tiempo, toques por WhatsApp y servicios fuera de cobertura no tienen meta en la hoja.
- [ ] KPIs: la hoja trae "Recolecciones por ruta al día" verde 10 sin rojo ni súper verde, y tickets firmados y visitas con foto con verde 100 %: con eso, cualquier entrega sin firma o visita sin foto pinta rojo. Confirmar.
- [ ] KPIs: qué significa el valor "rojo" de la meta. Hoy es rojo todo lo que no alcanza verde y el número rojo solo se muestra. Confirmar si entre rojo y verde debe haber un color intermedio (amarillo) o si "rojo" es el umbral de la alerta al responsable.
- [ ] KPIs: "fuera de tiempo" se mide contra la primera fecha de entrega programada; confirmar si debe ser contra los días de proceso de la línea de servicio. "Cobro anticipado" cuenta el cobro de referencia del chofer en la recolección; confirmar si también cuenta lo pagado en tienda o en línea.
- [ ] KPIs: alerta al responsable cuando su indicador cae en rojo; llega con el correo (avisos pendientes de canal).
- [ ] KPIs: NPS y encuestas respondidas quedan sin datos hasta la encuesta de la app del invitado (cuándo se manda, por qué canal y la pregunta).
- [ ] "Llamar hoy" no excluye a quien ya tiene una recolección agendada; la lista lo muestra ("ya tiene recolección el …"). Confirmar si debe salir de la lista.

Se escribieron en `03-reglas-de-negocio.md` como propuesta razonable; confirmar antes de darlas por cerradas:

- [ ] Recolectado → En planta ocurre al llegar el primer ticket del POS ligado al pedido.
- [ ] Recálculo de ruta cuando el chofer va más de 15 minutos retrasado.
- [ ] Definición de estatus "Le toca" frente a la próxima visita de zona.
- [ ] Ventana de una hora específica (por ejemplo ±30 minutos).
- [ ] Vigencia del código de verificación (10 minutos, 5 intentos).

## Fase 2

- Pago en línea y cobro anticipado desde la app del invitado.
- Integración completa con el POS para canjes y conciliación automática.
- BigQuery con HubSpot, pauta y nómina; costo de adquisición por fuente.
- Inventario de insumos.
- Si hace falta reducir alcance para mantener las 12 semanas: Promociones, Clientes y fuentes y la bandeja de WhatsApp.
