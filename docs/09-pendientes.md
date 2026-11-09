# Pendientes y fase 2

## Por confirmar con CleanCom

- [ ] Sistema POS y cómo llega hoy su información (API, base de datos o archivo).
- [ ] Base de datos que usa la app actual y respaldo para migrar.
- [ ] Excel máster.
- [ ] Características y sistema operativo del VPS.
- [ ] Tipos de pagos por unidad (tenencia o refrendo, crédito o arrendamiento, póliza en pagos, otros).
- [ ] Días de proceso prometidos por línea de servicio (planchaduría, lavandería, tintorería, sastrería, calzado).
- [ ] Días permitidos por etapa del pedido antes de marcarlo atrasado.
- [ ] Destinatarios de cada aviso de flota.

## Reglas propuestas que conviene validar

Se escribieron en `03-reglas-de-negocio.md` como propuesta razonable; confirmar antes de darlas por cerradas:

- [ ] Recolectado → En planta ocurre al llegar el primer ticket del POS ligado al pedido.
- [ ] Recálculo de ruta cuando el chofer va más de 15 minutos retrasado.
- [ ] Definición de estatus "Le toca" frente a la próxima visita de zona.
- [ ] Ventana de una hora específica (por ejemplo ±30 minutos).
- [ ] Vigencia del código de verificación (10 minutos, 5 intentos).

## Fase 2

- Pago en línea y cobro anticipado desde la app del invitado.
- Integración completa con el POS para canjes y conciliación automática.
- BigQuery con HubSpot, pauta y nómina; costo de adquisición por fuente.
- Inventario de insumos.
- Si hace falta reducir alcance para mantener las 12 semanas: Promociones, Clientes y fuentes y la bandeja de WhatsApp.
