# Pantallas

Una ficha por pantalla. Cada ficha dice qué hace, lo que **se conserva** del sistema actual, lo que **mejora**, los endpoints que necesita y los criterios para darla por terminada. Las reglas de negocio no se repiten aquí: están en [`../03-reglas-de-negocio.md`](../03-reglas-de-negocio.md).

## Web de oficina

| Sección | Pantalla | Ficha |
| --- | --- | --- |
| Logística | Panel del día | [oficina/panel-del-dia.md](oficina/panel-del-dia.md) |
| Logística | Rutas | [oficina/rutas.md](oficina/rutas.md) |
| Logística | Agendar | [oficina/agendar.md](oficina/agendar.md) |
| Logística | Pedidos | [oficina/pedidos.md](oficina/pedidos.md) |
| Logística | Clientes (CRM) y ficha | [oficina/clientes-crm.md](oficina/clientes-crm.md) |
| Logística | Paradas fijas | [oficina/paradas-fijas.md](oficina/paradas-fijas.md) |
| Logística | Promociones | [oficina/promociones.md](oficina/promociones.md) |
| Planta | Tickets en planta | [oficina/tickets-en-planta.md](oficina/tickets-en-planta.md) |
| Gerencia | KPIs | [oficina/kpis.md](oficina/kpis.md) |
| Gerencia | Clientes y fuentes | [oficina/clientes-y-fuentes.md](oficina/clientes-y-fuentes.md) |
| Gerencia | Evidencias y caja | [oficina/evidencias-y-caja.md](oficina/evidencias-y-caja.md) |
| Gerencia | Vista del chofer (vista previa) | [oficina/vista-del-chofer.md](oficina/vista-del-chofer.md) |
| Administración | Usuarios | [oficina/usuarios.md](oficina/usuarios.md) |
| Administración | Operación | [oficina/operacion.md](oficina/operacion.md) |
| Administración | Unidades | [oficina/unidades.md](oficina/unidades.md) |
| Administración | WhatsApp y metas | [oficina/whatsapp-y-metas.md](oficina/whatsapp-y-metas.md) |

## Apps

| App | Ficha |
| --- | --- |
| Chofer (tableta) | [apps/chofer.md](apps/chofer.md) |
| Invitado (autoservicio) | [apps/invitado.md](apps/invitado.md) |

## Lineamientos de interfaz

- Español de México; al cliente final se le llama "invitado".
- Colores de ruta consistentes en toda la app (F azul, K verde azulado) y el mismo código de etiquetas de tipo de parada (cliente final, convenio, insumos, traspaso).
- Estados escritos para personas ("No realizada"), nunca valores técnicos (`no_realizada`).
- Fechas como "mar 3 nov" y horas en formato de 24 horas o "p. m." de forma consistente.
- Toda acción irreversible o que regresa una etapa pide confirmación y motivo.
