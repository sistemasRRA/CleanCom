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

## Endpoints

- `GET /gerencia/clientes?periodo=&fecha=&tipo=`, `GET /gerencia/clientes/retencion`, `GET /gerencia/clientes/perdidos?periodo=`.
