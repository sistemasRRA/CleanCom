# KPIs

**Ruta actual:** `/gerencia` · **Rol:** Administración

Indicadores con metas de la hoja "KPI's 2026 CleanCom", por semana, mes o trimestre.

## Se conserva

- Selector de semana, mes o trimestre; navegación anterior, hoy, siguiente.
- Contadores de semáforo: súper verde, verde, rojo, sin meta.
- Tarjeta por indicador: valor, periodo anterior, desglose F y K, tendencia, meta (rojo, verde, súper verde) y responsable.
- Secciones Logística, Venta y Experiencia (lista de indicadores en reglas de negocio).
- Histórico de las últimas 8 semanas con color por semáforo.
- Aviso de que HubSpot, pauta y nómina no se calculan aquí.

## Mejora

- Los indicadores que hoy dicen "sin datos" se llenan con la app del chofer (llegadas a tiempo, foto de evidencia, tickets firmados) y la encuesta NPS de la app del invitado.
- Cálculo por proceso programado guardado por periodo y ruta.
- Metas con vigencia: cambiar una meta no reescribe semáforos pasados.
- Clic en un indicador para ver los pedidos, visitas o invitados que lo forman.
- Alerta al responsable cuando su indicador cae en rojo.

## Contexto

Recolecciones por ruta al día marca entre 0.9 y 2.1 contra una meta de 10 porque faltan operaciones por registrar. Facturación y ticket promedio vienen del POS.

## Endpoints

- `GET /kpis?periodo=&fecha=`, `GET /kpis/{clave}/detalle?periodo=&fecha=`, `GET /kpis/historico?semanas=8`.
