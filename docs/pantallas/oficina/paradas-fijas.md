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

## Cómo quedó

- **Parada y vuelta fija son el mismo registro:** se capturan los pasos en orden (lugar del catálogo o dirección con coordenadas, qué se hace y nota). Un paso es una parada fija; varios pasos son una vuelta, que genera un `Viaje` cuyos pasos se visitan seguidos y en orden. Botones ▲ ▼, "+ Otro paso" y "+ Regresar al inicio".
- **Tipo de parada** (cliente final, convenio, insumos, traspaso) y **qué se hace** (recolección, entrega, entrega y recolección, traslado), los mismos catálogos del sistema actual. Convenio: empresa o cliente con convenio y visita fija.
- **Lugar en la ruta:** automático (donde quede mejor dentro del turno), primera parada del día o última parada antes de Leones.
- **Link de Google Maps o coordenadas** en cada paso que no es lugar del catálogo: se leen las coordenadas del link completo o de "19.36, -99.19". Un link corto (maps.app.goo.gl) no trae coordenadas y se pide abrirlo y copiar el link completo; con Google conectado se cambia por autocompletado.
- **Por paso:** qué se hace y turno ("igual que la parada" o el suyo), **antes de** (hora límite que cierra su ventana para el ruteo), indicación ("Cargar en Leones") y nota. Los pasos van en orden, no necesariamente seguidos.
- Las tiendas 7clean (Florida, Del Valle, Polanco, Lomas) están en Operación › Tiendas y bodegas y se eligen como lugar del paso.
- **Turno:** por la mañana, por la tarde o cualquier hora; la hora específica y las ventanas cortas llegan con Rutas.
- **Generación:** al arrancar el API y cada noche a las 00:15 (hora de CDMX) se generan las fijas de hoy a `Fijas.DiasAnticipacion` días; correrla dos veces no duplica. Al guardar una fija, pausarla, desactivarla o cambiarle excepciones se reemplazan sus paradas desde hoy que el chofer no haya iniciado.
- **Cupo:** cada paso ocupa un lugar. La agenda cuenta también las fijas de días que todavía no se generan, así que nunca promete un lugar que una fija va a ocupar.
- **La semana de un vistazo:** ruta por día (lunes a sábado), color por tipo, turno, marca 1ª o Últ., días inhábiles y "3 de 14 lugares" por celda; clic para editar. "Agregar las fijas del día" regenera un día.
- **Saltar fechas** desde el formulario (excepciones); quitarlas es baja lógica.

## Endpoints

- `GET /paradas-fijas` (`?tipo=&inactivas=`), `GET /paradas-fijas/{id}`, `POST /paradas-fijas`, `PUT /paradas-fijas/{id}`. Una vuelta es una parada fija con varios pasos; no hay `/vueltas-fijas`.
- `POST /paradas-fijas/{id}/excepciones`, `DELETE /paradas-fijas/{id}/excepciones/{excepcionId}` (baja lógica).
- `GET /paradas-fijas/semana?fecha=`.
- `POST /paradas-fijas/generar?fecha=` (regenera un día; reemplaza solo lo que no se ha iniciado).

## Criterios de aceptación

- El Traspaso K (3 pasos, lunes a sábado) se genera solo, en orden, 7 días adelante.
- Una fecha marcada como inhábil o como excepción no genera la parada.
- Las fijas descuentan cupo en el motor de agenda.
