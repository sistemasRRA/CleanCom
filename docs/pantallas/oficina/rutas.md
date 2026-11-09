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

## Endpoints

- `GET /rutas/dia?fecha=`, `PUT /rutas/dia/{id}` (chofer, unidad).
- `POST /rutas/dia/{id}/optimizar`, `POST /rutas/dia/{id}/simular` (efecto de un cambio sin guardar).
- `POST /paradas`, `POST /viajes`, `PATCH /paradas/{id}` (posición, fijar, ventana).
- `POST /paradas-fijas/generar?fecha=`.

## Criterios de aceptación

- Un viaje de 3 pasos siempre queda en orden y seguido tras optimizar.
- Una parada fijada no cambia de posición al recalcular.
- Guardar una ruta con regreso posterior a 18:00 muestra la alerta de horas extra.
