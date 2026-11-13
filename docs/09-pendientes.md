# Pendientes y fase 2

## Por confirmar con CleanCom

- [ ] Sistema POS y cómo llega hoy su información (API, base de datos o archivo).
- [ ] Base de datos que usa la app actual y respaldo para migrar.
- [ ] Excel máster.
- [ ] Dirección de la base (`Base.Direccion`): dice "Los Alpes, 01710", pero en el catálogo oficial de Correos de México Los Alpes es el CP 01010 y el 01710 es Ampliación Alpes o Las Águilas. Confirmar cuál es la correcta.
- [ ] Características y sistema operativo del VPS.
- [ ] Tipos de pagos por unidad (tenencia o refrendo, crédito o arrendamiento, póliza en pagos, otros).
- [ ] Días de proceso prometidos por línea de servicio (planchaduría, lavandería, tintorería, sastrería, calzado).
- [ ] Días permitidos por etapa del pedido antes de marcarlo atrasado.
- [ ] Destinatarios de cada aviso de flota.
- [ ] Textos iniciales del catálogo de motivos (no entrega, atraso en planta, resultado de contacto). El catálogo existe pero arranca vacío.
- [ ] Permisos iniciales de Administración (todo menos Roles y permisos), Gerencia (ver todo menos Usuarios y Operación), Logística (rutas, paradas, pedidos, panel y unidades), Telemarketing (agendar, CRM, WhatsApp) y Planta (tickets en planta y ver pedidos). Son una propuesta; se ajustan en Usuarios › Roles y permisos.
- [ ] Equipo de los choferes: tableta de la empresa, celular (propio o de la empresa) o ambos; Android, iPhone o ambos. La app es responsiva, pero cambia cómo se distribuye.
- [ ] Distribución de la app del chofer (no se publica en búsquedas de las tiendas). Propuesta: Android con APK interno o prueba interna de Google Play; iOS con Ad Hoc mientras sean menos de 100 equipos y después como app no listada en el App Store. Cambios de JavaScript con EAS Update.
- [ ] Capacidad de prendas por ruta: Operación la pide "por ruta", pero las reglas la marcan como general (`Unidad.CapacidadPrendas`) y la unidad ya tiene la suya. Por ahora no se sobrescribe por ruta.

## Decisiones de implementación por revisar

- [ ] Restablecer contraseña por link o código (usuarios.md): necesita WhatsApp o correo (semana 6). Mientras, oficina asigna una contraseña temporal.
- [ ] Verificación en dos pasos para Administración: no está en la semana 1.
- [ ] Bitácora de cambios: hoy cada tabla guarda quién creó y quién modificó por última vez; falta el historial completo de pedidos, rutas, usuarios y metas.

- [ ] Agenda: el aviso "cita agendada" al invitado llega con las notificaciones (semana 6); hoy `agendar` crea el pedido sin avisar.
- [ ] Agenda: la propuesta de "ruta más factible" para direcciones fuera de cobertura llega con la optimización de Google (semana 3); mientras, oficina elige la zona a mano.
- [ ] Folio del pedido: falta definir su formato; por ahora se usa el id.
- [ ] Buscar y ver invitados pide permiso de Agendar; cuando exista la pantalla Clientes (CRM) se abre también a ese permiso.

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
