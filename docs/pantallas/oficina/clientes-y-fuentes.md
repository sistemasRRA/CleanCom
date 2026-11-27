# Clientes y fuentes

**Ruta actual:** `/gerencia/clientes` · **Rol:** Administración

De dónde vienen los invitados y cuánto venden: nuevos por fuente o campaña, recurrentes, recuperados y perdidos.

## Se conserva

- Filtro clientes finales, convenios o todos; periodo semana, mes, trimestre o año.
- Indicadores: nuevos (de marketing digital), recurrentes, recuperados, perdidos y venta, con invitados atendidos y activos al cierre.
- Tabla por fuente: cómo nos conoció, nuevos, venta de nuevos, atendidos, pedidos, venta, ticket promedio, % de la venta, perdidos.
- Tendencia de los últimos 6 periodos y definiciones al pie.

## Mejora

- Una sola definición de 120 días compartida con el CRM.
- Periodo en curso marcado como parcial y comparado contra los mismos días del periodo anterior.
- Retención por generación: de los nuevos de cada mes, cuántos repiten a 30, 60 y 90 días, por fuente.
- Clic en Perdidos para mandar la lista a una campaña o a "Llamar hoy".
- Fuente capturada sola en la app (link o código de campaña).
- Fase 2: costo por fuente con la pauta en BigQuery.

## Contexto

De junio a octubre de 2026 entraron 79 nuevos y volvieron 33, pero se perdieron 92; la base activa pasó de 135 a 147.

## Estado (semana 7, hecho)

Pantalla `/clientes-y-fuentes`. Reglas en [Clientes y fuentes](../../03-reglas-de-negocio.md#clientes-y-fuentes).

- Filtros: tipo (todos, clientes finales, convenios y empresas; nunca tiendas internas) y periodo (semana, mes, trimestre, año), con ‹ › para moverse y "Actual".
- Indicadores: nuevos, recurrentes, recuperados, perdidos (abre la lista), venta, atendidos y activos al cierre, cada uno con el valor del periodo con que se compara. El periodo en curso cuenta hasta hoy y se compara con los mismos días del anterior.
- Tabla por fuente (cómo nos conoció; "Sin capturar" si no tiene): nuevos, venta de nuevos, atendidos, pedidos, venta, ticket promedio, % de la venta y perdidos (clic abre los perdidos de esa fuente). Fila de total.
- Tendencia de los últimos 6 periodos.
- Retención por generación: nuevos de cada uno de los últimos 6 meses y cuántos volvieron a pedir a 30, 60 y 90 días, filtrable por fuente. El porcentaje es sobre quienes ya cumplieron esos días ("en curso" si faltan).
- Perdidos: quién, fuente, último pedido, desde cuándo es perdido, pedidos y venta histórica. Con permiso de editar, "Mandar a Llamar hoy" los pone en la lista "Llamar hoy" del CRM (reactivación) hasta que alguien registre un contacto con ellos; no se manda a quien pidió no ser contactado, a quien ya está en la lista ni a quien ya volvió.
- Definiciones al pie.
- Nuevos de marketing digital: los nuevos cuya fuente está marcada como "marketing digital" en Operación › Motivos (Cómo nos conoció); esas fuentes llevan la etiqueta "Digital" en la tabla. La campaña escrita después de « · » no separa la fuente.
- Pendiente: mandar a una campaña (llega con las campañas de WhatsApp) y fuente capturada sola en la app.

## Endpoints

- `GET /gerencia/clientes?periodo=&fecha=&tipo=` (Clientes y fuentes: View): indicadores, comparación, tabla por fuente y tendencia. `periodo`: Semana, Mes (por omisión), Trimestre o Anio; `tipo`: Todos, ClientesFinales o Convenios.
- `GET /gerencia/clientes/retencion?fecha=&tipo=&fuente=` (View): los 6 meses hasta el de `fecha`.
- `GET /gerencia/clientes/perdidos?periodo=&fecha=&tipo=&fuente=` (View).
- `POST /gerencia/clientes/perdidos/llamar` `{ invitadoIds }` (Edit): a "Llamar hoy".
