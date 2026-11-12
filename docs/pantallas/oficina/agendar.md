# Agendar

**Ruta actual:** `/logistica/agendar` · **Rol:** Administración

Captura de una recolección por teléfono o WhatsApp. La fecha la calcula el motor de agenda del API, el mismo que usa la app del invitado.

## Se conserva

- Buscar invitado por nombre o teléfono; si existe se precargan sus datos y los cambios actualizan su ficha ("Capturar otro cliente").
- Campos: nombre, WhatsApp, calle y número, CP, colonia, cómo nos conoció, campaña, link de Maps, referencias, cuándo prefiere (cualquier hora, mañana, tarde, hora específica), prendas aproximadas, hora de registro editable, notas para el chofer, prioridad.
- Panel "Así se agenda": zona y colonia, fecha calculada, explicación paso a paso (días de la zona, corte, primer día con cupo y lugar "9 de 14") y otros días de la zona con su ocupación.
- "Agendar y avisar al cliente".

## Mejora

- Dirección con autocompletado de Google: se obtienen colonia, CP y coordenadas exactas; el link de Maps ya no es necesario.
- Zona por coordenadas en lugar de por CP.
- Cambiar de día con un clic en los otros días de la zona.
- Elegir dirección entre las guardadas del invitado.
- Aviso automático por push o WhatsApp según el invitado; ya no se abre WhatsApp Web.
- Línea de servicio en la captura (tintorería, lavandería, planchaduría, sastrería, calzado).
- Aplicar un beneficio vigente del invitado.

## Endpoints

- `GET /invitados/buscar?q=` (nombre o teléfono en cualquier formato), `GET /invitados/{id}`.
- `POST /invitados` (con su primera dirección), `PUT /invitados/{id}`, `POST /invitados/{id}/direcciones`, `PUT /invitados/{id}/direcciones/{direccionId}`. Al guardar una dirección se le asigna su zona (polígono, código postal o la elegida a mano). Un WhatsApp repetido responde 409 con el nombre del invitado que ya lo tiene.
- `POST /agenda/disponibilidad` → zona y cómo se encontró, ruta, fecha mínima, fecha, lugar, otros días con ocupación y explicación. Acepta una dirección guardada o la que se está capturando (coordenadas o CP), y una zona elegida a mano.
- `POST /agenda/agendar` → pedido creado en Agendado con su parada de recolección, o `scheduled: false` con la siguiente fecha si el día se llenó mientras se capturaba. Con `date` se agenda en otro día de la lista.

## Criterios de aceptación

- Mismo resultado que la app del invitado para la misma dirección y hora.
- Registro a las 18:51 en zona K3 → no entra el mismo día.
- Dos personas no pueden tomar el último lugar del mismo día y ruta.
