# CLAUDE.md

Guía para Claude Code en este repositorio. Léela completa antes de escribir código.

## Qué es

Monorepo del sistema de CleanCom (tintorería a domicilio en CDMX): un API en C# y tres clientes (web de oficina, app del chofer, app del invitado). La especificación completa está en `docs/`; si algo de esta guía choca con `docs/03-reglas-de-negocio.md`, manda el documento de reglas y avisa de la contradicción.

## Reglas de trabajo

- **Toda regla de negocio vive en el API.** Agenda, cupos, cortes, zonas, estatus del CRM, etapas del pedido, promociones y KPIs se calculan en `src/api`. Web y apps solo muestran y envían datos. Si una pantalla necesita un cálculo, se agrega un endpoint.
- **Un solo motor de agenda.** La web (Agendar) y la app del invitado usan los mismos endpoints `disponibilidad` y `agendar`. Nunca dupliques esa lógica.
- **Valores configurables, nunca fijos en código.** Cortes (13:00, 12:00), cupo (14), meta (10), turnos, minutos por parada, umbrales del CRM (45 y 120 días), tolerancia GPS, etc. salen de la tabla de configuración de Operación. Ver `docs/03-reglas-de-negocio.md#configuración`.
- **Hora de Ciudad de México.** Todo corte y fecha de negocio se evalúa en `America/Mexico_City`. En base de datos se guarda UTC (`datetimeoffset` o `datetime2` en UTC). Usa un servicio `IRelojNegocio` inyectable para poder probar cortes.
- **Idioma.** Código (clases, métodos, variables, tablas) en inglés técnico es aceptable, pero los **términos de dominio se conservan en español** según `docs/glosario.md` (por ejemplo `Invitado`, `Parada`, `Ruta`, `Zona`, `Pedido`, `Ticket`). Textos de interfaz siempre en español de México. Al cliente final se le llama **invitado**.
- **Pruebas.** Cada regla de `docs/03-reglas-de-negocio.md` tiene al menos una prueba unitaria en el API, con los ejemplos que trae el documento (por ejemplo: cambio el día 2 a las 12:01 → nueva fecha día 4).
- **Migraciones.** Solo con EF Core (`dotnet ef migrations add`). No edites a mano los archivos de `database/migraciones`.
- **Secretos.** Nunca en el repositorio. Llaves de Google, Meta, Twilio, cadena de conexión y JWT van en variables de entorno o `dotnet user-secrets`. Hay un `.env.example` por proyecto.
- **Fotos y documentos** se guardan en disco del servidor (ruta configurable), nunca dentro de SQL Server, para no rebasar el límite de 10 GB de SQL Server Express.
- **Commits.** Mensajes de commit y descripciones de PR en inglés. El nombre del proyecto se escribe `CleanCom`, sin espacio. El autor es siempre el usuario configurado en git. No agregues líneas `Co-Authored-By: Claude`, `Claude-Session` ni ninguna otra atribución a Claude en mensajes de commit ni en descripciones de PR.

## Stack

| Proyecto | Tecnología |
| --- | --- |
| `src/api` | ASP.NET Core Web API .NET 10, EF Core, SQL Server (Express en el VPS, tipos `geography`), Hangfire, SignalR, ASP.NET Identity + JWT, FluentValidation, Serilog |
| `src/web` | React 19 + Vite + TypeScript, TanStack Query, React Router, Google Maps JS API, cliente SignalR |
| `src/mobile/chofer` | Expo (React Native) + TypeScript, diseño para tableta, SQLite local (offline), expo-location en segundo plano, expo-camera, Expo Notifications |
| `src/mobile/invitado` | Expo (React Native) + TypeScript, mapa con seguimiento del chofer, Expo Notifications |
| `src/mobile/shared` | Cliente del API tipado, componentes y utilidades comunes |

El cliente del API para web y apps se genera desde el OpenAPI del backend; no escribas a mano los tipos de las respuestas.

## Estructura esperada del API

```
src/api/
├── CleanCom.Api/            Endpoints, autenticación, SignalR hubs
├── CleanCom.Dominio/        Entidades y reglas puras (sin dependencias)
├── CleanCom.Aplicacion/     Casos de uso (AgendarRecoleccion, OptimizarRuta, ...)
├── CleanCom.Infraestructura/ EF Core, Google, WhatsApp, Twilio, POS, correo, archivos
├── CleanCom.Trabajos/       Procesos de Hangfire (fijas a 7 días, estatus CRM, KPIs, flota)
└── CleanCom.Pruebas/        xUnit
```

## Roles

`Administracion`, `Chofer` e `Invitado`. Los permisos se definen por pantalla para poder agregar roles después sin desarrollo. Ver `docs/pantallas/oficina/usuarios.md`.

## Antes de terminar una tarea

1. Compila y corre las pruebas del proyecto que tocaste.
2. Si cambiaste una regla o un endpoint, actualiza el documento correspondiente en `docs/`.
3. Si quedó algo pendiente o una duda de negocio, agrégala a `docs/09-pendientes.md` en lugar de inventar la regla.
