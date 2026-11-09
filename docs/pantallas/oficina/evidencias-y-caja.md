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

El cobro oficial se registra en el POS; lo que anota el chofer es referencia.

## Endpoints

- `GET /visitas?desde=&hasta=&ruta=&formaPago=`, `GET /cortes?fecha=`, `POST /cortes/{id}/cerrar`, `GET /pedidos/{id}/evidencias`.
