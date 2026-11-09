# Paradas fijas

**Ruta actual:** `/logistica/fijas` · **Rol:** Administración

El trabajo que se repite cada semana: convenios, insumos y traspasos. Se pide por turno y el ruteo lo acomoda.

## Se conserva

- "La semana de un vistazo": cuadrícula ruta por día (lunes a sábado), color por tipo (cliente final, convenio, insumos, traspaso), turno y marca de primera o última.
- Lista de todas las paradas fijas con filtro por tipo y "Ver inactivas": parada, tipo, dirección, ruta, días, turno, lugar en la ruta, Editar.
- Formulario: nombre, tipo de parada, qué se hace, ruta, turno, días, lugar en la ruta (automático, primera, última), dirección, link o coordenadas, insumos, contacto en sitio, teléfono, prendas aproximadas, vuelta y paso de la vuelta, notas para el chofer, activa.

## Mejora

- Generación automática cada noche para los próximos 7 días; el botón "Agregar las fijas del…" queda solo para regenerar un día.
- Vuelta fija como un solo registro con pasos en orden (igual que "Viaje de un día"), en lugar de ligar tramos por nombre y número de paso.
- Lugares del catálogo; convenios ligados a su empresa en el CRM.
- Vigencia (desde y hasta), saltar fechas puntuales y pausar sin desactivar.
- Cupo visible en la cuadrícula: lugares que ocupan las fijas de 14 por día.
- Editar con clic y arrastrar para mover de día o de ruta.

## Endpoints

- `GET /paradas-fijas`, `POST /paradas-fijas`, `PUT /paradas-fijas/{id}`.
- `GET /vueltas-fijas`, `POST /vueltas-fijas`, `PUT /vueltas-fijas/{id}`.
- `POST /paradas-fijas/{id}/excepciones`.
- `GET /paradas-fijas/semana`.

## Criterios de aceptación

- El Traspaso K (3 pasos, lunes a sábado) se genera solo, en orden, 7 días adelante.
- Una fecha marcada como inhábil o como excepción no genera la parada.
- Las fijas descuentan cupo en el motor de agenda.
