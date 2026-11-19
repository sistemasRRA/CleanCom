# Rutas

**Ruta actual:** `/logistica/rutas` · **Rol:** Administración

Planeación del día por ruta: chofer, unidad y paradas en orden, con horas estimadas y mapa.

## Se conserva

- Por ruta: chofer, unidad, paradas, km y hora de regreso.
- Salida de Leones (carga de la unidad, 09:00) y entrega de ruta en Leones (descarga) como inicio y fin.
- Por parada: número de orden, nombre, dirección, hora estimada, tipo (recolección, entrega, entrega y recolección), convenio, turno (mañana, tarde), paso de viaje, restricciones ("Antes: a las 2:00 p.m.", "Dentro del estacionamiento"), y aviso "Fuera de su turno".
- Comida del chofer de 1 hora.
- Botones ▲ ▼, Mapa y Cambiar por parada; mapa con los números del orden de visita.
- Agregar paradas fijas del día, agregar viaje entre tiendas o desde bodega ("Viaje de un día") y agregar parada de insumos o traspaso ("Parada de un día").
- "Viaje de un día": ruta, tipo, turno, lugar en la ruta, qué se lleva y pasos (lugar del catálogo, qué se hace, nota), con "+ Otro paso" y "+ Regresar al inicio".
- "Parada de un día": ruta, tipo (convenio, insumos, traspaso), turno, lugar en la ruta, qué es, insumos o carga y dirección.
- Recálculo del orden con cada cambio y, en ruta, desde donde va el chofer.

## Mejora

- Optimización con Google Route Optimization respetando ventanas, pasos en orden, comida, inicio y fin en la base y posiciones fijas.
- Bloqueo o aviso antes de guardar: horas extra (hoy la Ruta F regresa a las 19:36) y paradas fuera de ventana (hoy Wildress Roma queda a las 18:24 con límite a las 4:00 p.m.).
- Tablero de planeación: columna por ruta más paradas sin asignar; arrastrar entre rutas y ver el efecto en horas y km al momento.
- Vista de línea de tiempo por ruta con las ventanas.
- Fijar posición: una parada movida a mano queda fija.
- Trazo por calles reales con Routes API.
- Un solo botón "Agregar" con selector de tipo; lugares del catálogo y direcciones con autocompletado de Google, sin pegar links.
- Aviso de capacidad cuando las prendas estimadas pasan las 140 de la unidad.
- Aprobar o rechazar solicitudes de cambio de orden que mande el chofer.

## Cómo quedó (semana 3, sin Google)

- **Plan por ruta y día** (`GET /rutas/dia?fecha=`): chofer (el asignado ese día o el operador cuya ruta habitual es), paradas en orden con hora estimada, espera, km desde la anterior y ventana; salida de Leones (`Ruta.SalidaBase`), comida del chofer (`Comida.Desde` y `Comida.Minutos`) y entrega de ruta en Leones.
- **Estimación provisional:** distancia en línea recta × `Ruta.FactorDistancia` a `Ruta.VelocidadPromedio` km/h; minutos por parada de Operación (recolección 15, entrega 10, entrega y recolección 25, traslado 12). Al conectar Google se cambia por Routes API y Route Optimization sin tocar la pantalla.
- **Ventanas:** mañana y tarde según `Turno.Manana` y `Turno.Tarde`; cualquier hora = toda la jornada; hora específica ± `Parada.VentanaHoraEspecifica`; "Antes de" la cierra antes.
- **Optimizar** (`POST /rutas/dia/{id}/optimizar`), orden propio mientras llega Google: las iniciadas primero, luego las fijadas como primera, las demás por la siguiente más cercana dando prioridad a la ventana que cierra antes, y las fijadas como última. Los pasos de un viaje respetan su orden. Una parada movida a mano conserva su posición.
- **▲ ▼ y Fijar** (`PATCH /paradas/{id}`): mover una parada la deja fija; un paso de viaje no puede quedar antes de su paso anterior.
- **Alertas:** horas extra (regreso después de `Ruta.RegresoMaximo`, o del de sábado), parada fuera de su ventana, prendas sobre `Unidad.CapacidadPrendas`, más paradas que el cupo, sin chofer, sin coordenadas y paradas nuevas sin acomodar.
- **Chofer del día** (`PUT /rutas/{rutaId}/dias/{fecha}/chofer`): vacío regresa al habitual; un operador no puede tener dos rutas el mismo día. La app del chofer ve exactamente el mismo orden.
- **Agregar** (un solo botón con selector): **parada de un día** (`POST /paradas`, un lugar) o **viaje de un día** (`POST /viajes`, dos o más pasos en orden, con "+ Otro paso" y "+ Regresar al inicio"). Se capturan igual que las paradas fijas: tipo, qué se hace, turno, lugar en la ruta, insumos o qué se lleva y, por paso, lugar o dirección con link de Maps, qué se hace, turno, "antes de", indicación y nota. Ocupan cupo; pasarse del cupo sale en las alertas. No se agregan a días pasados ni inhábiles.
- **Quitar** (`DELETE /paradas/{id}`, baja lógica): quita la parada ese día; un paso quita todo su viaje. No aplica a la recolección de un pedido (se elimina desde la agenda) ni a lo que el chofer ya inició. Una parada fija quitada un día no regresa con la generación nocturna; "Agregar las fijas del día" la repone.
- **Acomodo:** las rutas van lado a lado en pantallas anchas (F a la izquierda, K a la derecha, en orden de código) y una debajo de otra en pantallas angostas; el mapa del día va abajo de ambas.
- **Mapa del día** (OpenStreetMap mientras no estén las llaves de Google): la base (B, `Base.Ubicacion`, que `GET /rutas/dia` manda en `baseLat` y `baseLng`) y las paradas de cada ruta con su número de orden y el color de la ruta, unidas en ese orden desde la base y de regreso, en línea recta. Al pasar el mouse sobre una parada se ve el nombre, la dirección, la hora estimada y la ventana; las cerradas se ven más claras. Botones por ruta para mostrarla u ocultarla; avisa cuántas paradas no tienen ubicación. Se actualiza al reordenar u optimizar.
- **Pendiente en esta pantalla:** trazo por calles (con Google), tablero para arrastrar entre rutas, simular, unidad y solicitudes de cambio del chofer.

## Endpoints

- `GET /rutas/dia?fecha=`, `PUT /rutas/{rutaId}/dias/{fecha}/chofer` (la unidad llega en la semana 5).
- `POST /rutas/dia/{id}/optimizar`, `POST /rutas/dia/{id}/simular` (efecto de un cambio sin guardar).
- `PATCH /paradas/{id}` (posición, fijar), `POST /paradas` (parada de un día), `POST /viajes` (viaje de un día), `DELETE /paradas/{id}` (quitar, baja lógica). Pendiente: ventana editable en la ruta.
- `POST /paradas-fijas/generar?fecha=`.

## Criterios de aceptación

- Un viaje de 3 pasos siempre queda en orden tras optimizar (seguido cuando sus horas lo piden, como el Traspaso K).
- Una parada fijada no cambia de posición al recalcular.
- Guardar una ruta con regreso posterior a 18:00 muestra la alerta de horas extra.
