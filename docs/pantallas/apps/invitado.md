# App del invitado

**Plataforma:** Expo (React Native), Android y iOS. **Rol:** Invitado.

Autoservicio como Uber o DiDi, pero para tintorería: el invitado pide su servicio solo, sigue a su chofer en el mapa y ve cada etapa de su pedido. Usa el mismo motor de agenda que la pantalla Agendar de oficina.

## Registro y acceso

- Celular + código por WhatsApp (SMS de respaldo con Twilio).
- Si el teléfono ya existe en el CRM, se liga a esa ficha y el invitado confirma sus datos y direcciones; si no, se crea con fuente "App" y se pregunta una vez "¿cómo nos conociste?".
- También puede entrar desde el link de "Invitar a la app" que manda oficina.
- Fuente o campaña capturada del link o código con el que llegó.

## Pedir servicio

1. Dirección guardada o ubicación actual marcada en el mapa (autocompletado de Google).
2. Línea de servicio, prendas aproximadas y notas para el chofer.
3. Fecha propuesta por el motor (primera disponible de su zona), con opción de otro día disponible y turno (mañana, tarde o cualquier hora). No hay hora específica ni prioridad.
4. Beneficios vigentes para aplicar.
5. Confirmar: aviso por push; el pedido aparece en Pedidos de oficina.

## Seguimiento

- Chofer en camino: mapa con la ubicación del chofer y hora estimada; botón para llamar o escribir.
- Etapas del pedido con aviso en cada cambio.
- Encuesta NPS al entregar.

## Cambios y cancelación

- Cambiar día: antes de las 12:00 → desde el día siguiente; a partir de las 12:00 → desde dos días después.
- Cancelar: hasta las 13:00 del día anterior a la recolección; después, el botón indica que debe contactar a oficina.
- Siempre en hora de Ciudad de México, sin importar la zona horaria del teléfono.
- La fecha y el turno de la entrega no se eligen ni se cambian desde la app (decisión de CleanCom, 24 nov 2026): dependen de la operación y la demanda, y los programa oficina con "Programar entrega". El invitado ve la fecha en su seguimiento.

## Otras secciones

- **Mis beneficios:** sellos y cupones con vencimiento y código.
- **Mis pedidos:** historial con tickets.
- **Mis direcciones** y **Mis datos**.
- **Avisos:** preferencias de push, WhatsApp de servicio y WhatsApp de promociones.
- Empresas con convenio: cada contacto con su cuenta, ligado a la ficha de la empresa.

## Endpoints

- `POST /auth/codigo`, `POST /auth/verificar` (hecho): código de 6 dígitos; solo se guarda su hash. Vale `Acceso.CodigoMinutos` (10), con `Acceso.CodigoIntentos` (5), uno nuevo tras `Acceso.CodigoReenvioSegundos` (60) y hasta `Acceso.CodigosPorHora` (5); además 10 peticiones por minuto por IP. Un celular que ya está en el CRM se liga a ese invitado; si no, se crea con fuente "App" (o la campaña del link) y `newInvitado: true` para pedir nombre y "¿cómo nos conociste?". La primera vez se le crea un usuario con rol Invitado. Mientras no esté WhatsApp (semana 6), en desarrollo el código se escribe en el log del API (`AccessCodes:LogToConsole`); en otro ambiente el API responde que el acceso con celular aún no está disponible.
- `GET|PUT /yo`, `GET|POST|PUT|DELETE /yo/direcciones` (hecho): sin notas de oficina; el celular no se cambia; la zona sale de la dirección (una zona puesta a mano por oficina se conserva). `profileComplete` dice si ya dio su nombre y cómo nos conoció.
- `POST /agenda/disponibilidad`, `POST /agenda/agendar` (hecho; mismos que oficina, con reglas de rol Invitado): solo para sí y sus direcciones; sin hora específica, prioridad, zona a mano ni hora de registro (siempre ahora); el pedido queda con origen App.
- `GET /yo/pedidos`, `GET /yo/pedidos/{id}/seguimiento` (hecho): etapas con su hora y el estado de la visita de hoy; solo los propios (otro responde 404). Mientras la visita está en camino trae también `chofer`: nombre de pila, ruta y color, última posición con su hora y `signalLostAt`, la dirección en el mapa y `estimatedArrival` ([reglas · GPS del chofer](../../03-reglas-de-negocio.md#gps-del-chofer)); fuera de "en camino" es `null`. También `canChangeDay`, `canCancel` y `cancelUntil`, que deciden qué botones muestra la app.
- `GET /yo/pedidos/{id}/cambiar-dia`, `POST /yo/pedidos/{id}/cambiar-dia` `{ date }`, `POST /yo/pedidos/{id}/cancelar` `{ note }` (hecho, semana 6): los días con lugar desde la fecha mínima del cambio (sin el día actual); el cambio usa el mismo motor y candado de la agenda y, si el día se llenó mientras tanto, responde `scheduled: false` con el siguiente; cancelar después del corte responde 400 con "comunícate con oficina". Reglas en [Cambios y cancelaciones](../../03-reglas-de-negocio.md#cambios-y-cancelaciones-desde-la-app-del-invitado).
- `GET /yo/beneficios` (hecho, semana 6): sellos y cupones con su código, estado (disponible, apartado, usado, vencido) y vencimiento. `POST /agenda/agendar` recibe `beneficioIds` para apartarlos en el pedido.
- `POST /yo/encuestas/{id}`.
- Ubicación del chofer en vivo: la app vuelve a pedir el seguimiento cada 10 s mientras va en camino (y cada 30 s el día de la visita, para enterarse de que salió). No se usa un hub de SignalR: el chofer manda su ubicación cada 30 a 60 s, así que el retraso es el mismo y en el celular una consulta periódica es más robusta que una conexión abierta.

## Cómo quedó (semana 5)

- **Acceso:** bienvenida → celular → código de 6 dígitos (se verifica solo al escribir el sexto, con cuenta regresiva para pedir otro y "Cambiar el número"). La cuenta sigue al celular: si oficina pasa ese número a otro invitado, la cuenta se liga al nuevo.
- **Perfil:** un invitado nuevo (o del CRM sin "¿cómo nos conociste?") da su nombre, correo opcional y elige una opción del catálogo de oficina antes de entrar. Después se edita en Mi cuenta.
- **Inicio:** "Pedir servicio" y los pedidos en curso.
- **Pedir servicio:** dirección guardada u "Otra dirección" (código postal → colonias y alcaldía del catálogo oficial; la zona la pone el API), líneas de servicio, prendas aproximadas y notas para el chofer, turno (mañana, tarde o cualquier hora), primer día disponible de su zona u otro con lugar, y Confirmar. Fuera de cobertura lo dice y no deja confirmar. Si el día se llenó mientras tanto, propone el siguiente.
- **Mis pedidos y seguimiento:** etapas con su hora, "Tu visita es hoy" / "va en camino" / "llegó" el día de la visita.
- **Mi cuenta:** mis datos, mis direcciones (agregar, editar con el lápiz, quitar), modo oscuro y cerrar sesión. En "Pedir servicio" cada dirección también tiene su lápiz.
- **Dirección en el mapa:** al agregar o editar, "¿Dónde está en el mapa?" con "Buscar mi dirección" (`GET /direcciones/ubicar`, OpenStreetMap; dice qué tan preciso fue), "Usar mi ubicación" (pide permiso de ubicación solo al tocarlo) y el mapa para tocar o arrastrar el pin. La ubicación se guarda con la dirección: decide la zona y es el destino del chofer en el seguimiento. Es opcional; sin ella la zona sale del código postal.
- **Chofer en el mapa (semana 6):** con la visita en camino, el seguimiento muestra "Luis va en camino", "Llega aproximadamente a las 11:24 a.m." y un mapa con el chofer (en el color de su ruta, gris sin señal) y la casa; se actualiza solo y al marcar Llegué cambia a "Tu chofer llegó". Apple Maps en iPhone y Google Maps en Android (`react-native-maps`); en Expo web, solo para desarrollo, el mapa de OpenStreetMap. Falta el botón para llamar o escribir (ver pendientes).
- **Cambiar día y cancelar (semana 6):** en el seguimiento de una recolección que falta por hacer, "¿Necesitas cambiar algo?" con "Cambiar día" (los días con lugar desde la fecha mínima, como chips) y "Cancelar pedido" (confirmación en la misma pantalla con "¿Por qué? (opcional)"), y hasta cuándo se puede cancelar. Pasado el corte, en lugar del botón dice que hay que comunicarse con oficina.
- **Beneficios (semana 6):** "Mis beneficios" con los disponibles y apartados (código para mostrar en oficina) y los usados y vencidos. En "Pedir servicio", el paso Beneficios lista los disponibles; elegir un cupón de una línea que no estaba la agrega al servicio. Quedan usados al recoger.
- Falta (semana 6): avisos push y por WhatsApp (el código hoy solo se ve en el log del API en desarrollo). Un invitado que ya estaba en el CRM entra directo sin el paso de "confirmar sus datos y direcciones".

## Criterios de aceptación

- Un invitado no puede ver datos ni ubicación de pedidos que no son suyos.
- La ubicación del chofer solo se muestra mientras su parada está "en camino".
- Cambio el día 2 a las 12:01 → la fecha más próxima ofrecida es el día 4.
