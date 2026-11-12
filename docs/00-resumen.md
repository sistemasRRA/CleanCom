# Resumen

CleanCom se reconstruye en **12 semanas**: 8 de desarrollo y 4 de pruebas, piloto y publicación en App Store y Google Play.

## Tres lados, un backend

- **Oficina (web):** logística, agenda, rutas, pedidos, CRM, planta, gerencia y administración.
- **Chofer (app en tableta o celular):** ruta del día, visitas, evidencias, kilometraje; funciona sin señal.
- **Invitado (app):** autoservicio tipo Uber/DiDi para pedir recolecciones, seguir al chofer en el mapa, ver sus pedidos y beneficios.

Todo se apoya en un API en C# (.NET 10) con SQL Server, alojado en el VPS que CleanCom ya tiene. BigQuery queda para reportes en fase 2.

## Equipo y plazo

El plan supone desarrollo de tiempo completo (backend, web y móvil) apoyados en Claude Code. Con menos capacidad, el mismo alcance toma unas 16 semanas, o se mantienen las 12 pasando Promociones, Clientes y fuentes y la bandeja de WhatsApp a fase 2.

## Costo de operación

Alrededor de **30 a 150 USD al mes** con 2 rutas (WhatsApp, Google Maps y SMS de respaldo), más 99 USD al año de Apple y 25 USD una sola vez de Google Play. Detalle en [08-costos.md](08-costos.md).

## Principios del rediseño

1. **Conservar lo que funciona.** El sistema actual ya resuelve agenda por zona, cupos, paradas fijas, CRM por frecuencia, tickets de planta, KPIs con metas y flota. Cada ficha de pantalla distingue lo que se conserva de lo que mejora.
2. **Que la operación se registre sola.** Hoy muchos KPIs dicen "sin datos" porque faltan operaciones por registrar. La app del chofer registra cada visita, foto, firma, hora de llegada y kilometraje al momento.
3. **Una sola fuente de reglas.** Agenda, cupos y cortes viven en el API; oficina y la app del invitado usan el mismo motor.
4. **Avisos automáticos.** WhatsApp por la API de Meta y notificaciones push reemplazan el envío manual uno por uno.

## Documentos

| Documento | Contenido |
| --- | --- |
| [01-arquitectura.md](01-arquitectura.md) | Capas, tecnologías y estructura del código |
| [02-modelo-de-datos.md](02-modelo-de-datos.md) | Entidades y relaciones |
| [03-reglas-de-negocio.md](03-reglas-de-negocio.md) | Agenda, cortes, zonas, rutas, pedidos, CRM, promociones, KPIs |
| [04-integraciones.md](04-integraciones.md) | Google Maps, WhatsApp, SMS, push, correo, POS |
| [05-plan-de-trabajo.md](05-plan-de-trabajo.md) | 12 semanas por frente e hitos |
| [06-migracion-de-datos.md](06-migracion-de-datos.md) | Excel máster, respaldo del sistema actual, limpieza |
| [07-despliegue-vps.md](07-despliegue-vps.md) | Servidor, respaldos, dominios |
| [08-costos.md](08-costos.md) | Requisitos y costos de servicios externos |
| [09-pendientes.md](09-pendientes.md) | Decisiones abiertas y fase 2 |
| [glosario.md](glosario.md) | Términos del negocio |
| [pantallas/](pantallas/) | Ficha por pantalla |
