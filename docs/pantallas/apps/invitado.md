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

- `POST /auth/codigo`, `POST /auth/verificar`.
- `GET|PUT /yo`, `GET|POST|PUT|DELETE /yo/direcciones`.
- `POST /agenda/disponibilidad`, `POST /agenda/agendar` (mismos que oficina, con reglas de rol Invitado).
- `GET /yo/pedidos`, `GET /yo/pedidos/{id}/seguimiento`, `POST /yo/pedidos/{id}/cambiar-dia`, `POST /yo/pedidos/{id}/cancelar`.
- `GET /yo/beneficios`, `POST /yo/encuestas/{id}`.
- Hub SignalR `seguimiento` (ubicación del chofer de su pedido mientras está en camino).

## Criterios de aceptación

- Un invitado no puede ver datos ni ubicación de pedidos que no son suyos.
- La ubicación del chofer solo se muestra mientras su parada está "en camino".
- Cambio el día 2 a las 12:01 → la fecha más próxima ofrecida es el día 4.
