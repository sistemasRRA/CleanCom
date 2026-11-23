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
- `GET /yo/pedidos`, `GET /yo/pedidos/{id}/seguimiento` (hecho): etapas con su hora y el estado de la visita de hoy; solo los propios (otro responde 404). `POST /yo/pedidos/{id}/cambiar-dia`, `POST /yo/pedidos/{id}/cancelar` (semana 6).
- `GET /yo/beneficios`, `POST /yo/encuestas/{id}`.
- Hub SignalR `seguimiento` (ubicación del chofer de su pedido mientras está en camino).

## Cómo quedó (semana 5)

- **Acceso:** bienvenida → celular → código de 6 dígitos (se verifica solo al escribir el sexto, con cuenta regresiva para pedir otro y "Cambiar el número"). La cuenta sigue al celular: si oficina pasa ese número a otro invitado, la cuenta se liga al nuevo.
- **Perfil:** un invitado nuevo (o del CRM sin "¿cómo nos conociste?") da su nombre, correo opcional y elige una opción del catálogo de oficina antes de entrar. Después se edita en Mi cuenta.
- **Inicio:** "Pedir servicio" y los pedidos en curso.
- **Pedir servicio:** dirección guardada u "Otra dirección" (código postal → colonias y alcaldía del catálogo oficial; la zona la pone el API), líneas de servicio, prendas aproximadas y notas para el chofer, turno (mañana, tarde o cualquier hora), primer día disponible de su zona u otro con lugar, y Confirmar. Fuera de cobertura lo dice y no deja confirmar. Si el día se llenó mientras tanto, propone el siguiente.
- **Mis pedidos y seguimiento:** etapas con su hora, "Tu visita es hoy" / "va en camino" / "llegó" el día de la visita.
- **Mi cuenta:** mis datos, mis direcciones (agregar, editar, quitar), modo oscuro y cerrar sesión.
- Falta (semana 6): beneficios, cambiar día y cancelar, chofer en el mapa, avisos push y por WhatsApp (el código hoy solo se ve en el log del API en desarrollo). Un invitado que ya estaba en el CRM entra directo sin el paso de "confirmar sus datos y direcciones".

## Criterios de aceptación

- Un invitado no puede ver datos ni ubicación de pedidos que no son suyos.
- La ubicación del chofer solo se muestra mientras su parada está "en camino".
- Cambio el día 2 a las 12:01 → la fecha más próxima ofrecida es el día 4.
