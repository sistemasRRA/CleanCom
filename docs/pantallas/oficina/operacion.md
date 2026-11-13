# Operación

**Ruta actual:** `/admin/operacion` · **Rol:** Administración

Reglas que mueven la agenda y el ruteo; todo cambio se aplica de inmediato. Los valores y su significado están en [reglas de negocio · Configuración](../../03-reglas-de-negocio.md#configuración).

## Se conserva

- Horarios y reglas: hora de corte (13:00), salida de Leones (09:00), regreso máximo (18:00), regreso los sábados (14:00), turno mañana (09:00 a 13:00), turno tarde (13:00 a 18:00), comida (60 min desde 13:00), meta de recolecciones (10), cupo de paradas (14), capacidad de la unidad (140 prendas), días de proceso en tienda (3), minutos por recolección (15), entrega (10) y traslado o insumos (12), tolerancia GPS (300 m).
- Dirección y coordenadas de la base.
- Foto obligatoria en cada visita; el chofer registra las paradas en orden.
- Tiendas y bodegas: nombre, tipo (base, bodega / insumos, tienda 7clean), dirección; agregar y quitar.
- Días de visita por zona (lunes a domingo), con nota de que las EXT siguen a su núcleo.
- Días inhábiles con fecha y motivo.
- Cobertura: CP, colonia, alcaldía y zona; búsqueda y alta (156 registros).
- Códigos postales: catálogo oficial de Correos de México (colonias y alcaldía por CP). SuperUser y Administración suben el TXT que se descarga de correosdemexico.gob.mx (por estado o todos); se puede repetir y lo que ya no venga se da de baja. Sirve para llenar colonia y alcaldía al capturar direcciones; la zona la sigue decidiendo Cobertura. El archivo no se guarda en el repositorio: sus términos prohíben distribuirlo.

## Mejora

- Reglas nuevas: corte de cambio de día (12:00), corte de cancelación (13:00 del día anterior), anticipación de fijas (7 días), umbrales del CRM (45 y 120 días), días de proceso por línea, días permitidos por etapa del pedido.
- Valores por ruta (cupo, regreso, capacidad).
- Tiempos aprendidos: sugerencia de minutos por tipo de parada con el promedio real medido por la app.
- "Llegué" sugerido al entrar al radio de la tolerancia GPS.
- Historial de cambios y cambios programados a partir de una fecha.
- Editor de zonas en mapa: dibujar polígonos partiendo de la cobertura por CP.
- Días de visita corregidos al máster en la migración (zona 1 lunes y jueves, zona 2 martes y viernes, zona 3 miércoles y sábado).

## Endpoints

- `GET /configuracion`, `PUT /configuracion` (con fecha de vigencia opcional), `GET /configuracion/historial?key=`.
- `GET|POST|PUT|DELETE /lugares`, `/zonas`, `/dias-inhabiles`, `/cobertura`; `PUT /zonas/{id}/dias`.
- `GET|POST|PUT /rutas`, `/tipos-parada`, `/lineas-servicio`, `/motivos?type=`.
- `GET /codigos-postales/{cp}` (colonias del CP), `GET /codigos-postales?search=`, `GET /codigos-postales/resumen`: solo sesión. `POST /codigos-postales/importar` (multipart, campo `file`): solo SuperUser y Administración.

Leer catálogos solo pide sesión iniciada (los usan todas las pantallas y apps); cambiarlos y ver la configuración pide permiso sobre Operación. `DELETE` siempre es baja lógica (lugares, zonas, días inhábiles y cobertura); nada se borra de la base. Los polígonos se envían como lista de vértices `{lat, lng}` y el API los cierra y orienta para `geography`.
