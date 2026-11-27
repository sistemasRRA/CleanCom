# Evidencias y caja

**Ruta actual:** `/gerencia/evidencias` · **Rol:** Administración

Cada visita con su foto, ticket, monto y forma de pago, y el corte de caja por chofer.

## Se conserva

- Filtros: desde, hasta, ruta, forma de pago.
- Total cobrado y visitas realizadas.
- Lista de visitas con foto, ticket, monto y forma de pago.

## Mejora

- Evidencia con hora, ubicación GPS y marca si se tomó a más de 300 m de la parada; firma del invitado cuando aplique.
- Corte de caja formal: el chofer declara al cerrar turno; oficina compara contra lo registrado y contra el POS, anota diferencias y cierra con folio y responsable.
- Galería por pedido con fotos de recolección y entrega.
- Exportación a Excel.
- Fase 2: cobro anticipado con link de pago.

## Regla

El cobro oficial se registra en el POS; lo que anota el chofer es referencia. Reglas del corte en [Cobro](../../03-reglas-de-negocio.md#cobro).

## Estado (semana 7, hecho)

Pantalla `/evidencias-y-caja` con dos vistas:

- **Visitas:** filtros desde, hasta (hoy por omisión, hasta 92 días), ruta y forma de pago. Totales de todas las páginas: realizadas, no realizadas, cobrado (referencia), cobrado por forma de pago y cuántas visitas tienen una foto o llegada fuera de radio (más de `Gps.ToleranciaMetros`). Tabla de 25 por página con fecha y hora de cierre, ruta y chofer, invitado o parada, tipo, prendas, pedido (abre su galería), tickets, pago, fotos (en rojo las de fuera de radio, "Sin foto" si no tiene), firma, notas o motivo de no realizada. "Exportar a Excel" descarga las mismas visitas (todas las páginas, hasta 20,000).
- **Corte de caja:** un día a la vez. Por cada ruta con turno, cobros o corte: chofer, unidad, horas y kilometraje del turno, estado (Sin turno, Turno abierto, Por revisar, Cerrado) y por forma de pago lo anotado en las visitas, lo declarado por el chofer, el punto de venta y las dos diferencias (negativa = falta dinero). "Revisar y cerrar" (permiso de editar) pide el monto del punto de venta por forma de pago (opcional) y notas; el API exige notas si hay diferencias y da el folio. Un corte cerrado muestra folio, quién lo cerró, cuándo y sus notas.
- **Reabrir corte** (permiso especial "Reabrir un corte de caja cerrado"): con el motivo; vuelve a "Por revisar" con sus montos del punto de venta y notas, y muestra quién lo reabrió y por qué.
- **Cerrar turno por el chofer** (permiso especial "Cerrar el turno de un chofer desde oficina"), en una ruta con turno abierto: lo entregado por forma de pago, kilometraje final opcional y por qué; avisa si quedan paradas sin cerrar. El corte muestra "Turno cerrado desde oficina por …".
- **Galería del pedido:** las fotos y la firma de cada visita del pedido (recolección y entrega), con la marca de fuera de radio.

## Endpoints

- `GET /visitas?desde=&hasta=&ruta=&formaPago=&pagina=&porPagina=` (Evidencias y caja: View): visitas cerradas (realizadas y no realizadas; sin salida, regreso ni comida), de la más reciente a la más vieja, con totales.
- `GET /visitas/exportar?desde=&hasta=&ruta=&formaPago=` (View): las mismas como archivo de Excel.
- `GET /pedidos/{id}/evidencias` (Pedidos o Evidencias y caja: View): galería del pedido.
- `GET /cortes?fecha=` (View): cortes del día con sus diferencias, y cuántos están por revisar.
- `POST /cortes/{id}/cerrar` `{ pos: [{ method, amount }], notes }` (Edit): cierra con folio `CC-AAAAMMDD-RUTA` y responsable. Lo anotado en las visitas queda como estaba al cerrar.
- `POST /cortes/{id}/reabrir` `{ reason }` (permiso `reabrir-cortes`: Edit).
- `POST /rutas/dia/{id}/turno/cerrar` `{ km?, declared: [{ method, amount }], notes, confirm }` (permiso `cerrar-turnos`: Edit): 409 pide confirmar un turno largo o paradas sin cerrar.
- `GET /evidencias/{id}/archivo` se abre con permiso de ver Pedidos o Evidencias y caja.
