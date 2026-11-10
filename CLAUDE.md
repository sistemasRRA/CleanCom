# CLAUDE.md

Guía para Claude Code en este repositorio. Léela completa antes de escribir código.

## Qué es

Monorepo del sistema de CleanCom (tintorería a domicilio en CDMX): un API en C# y tres clientes (web de oficina, app del chofer, app del invitado). La especificación completa está en `docs/`; si algo de esta guía choca con `docs/03-reglas-de-negocio.md`, manda el documento de reglas y avisa de la contradicción.

## Reglas de trabajo

- **Toda regla de negocio vive en el API.** Agenda, cupos, cortes, zonas, estatus del CRM, etapas del pedido, promociones y KPIs se calculan en `src/api`. Web y apps solo muestran y envían datos. Si una pantalla necesita un cálculo, se agrega un endpoint.
- **Un solo motor de agenda.** La web (Agendar) y la app del invitado usan los mismos endpoints `disponibilidad` y `agendar`. Nunca dupliques esa lógica.
- **Valores configurables, nunca fijos en código.** Cortes (13:00, 12:00), cupo (14), meta (10), turnos, minutos por parada, umbrales del CRM (45 y 120 días), tolerancia GPS, etc. salen de la tabla de configuración de Operación. Ver `docs/03-reglas-de-negocio.md#configuración`.
- **Hora de Ciudad de México.** Todo corte y fecha de negocio se evalúa en `America/Mexico_City`. En base de datos se guarda UTC (`datetimeoffset` o `datetime2` en UTC). Usa el servicio inyectable `IBusinessClock` para poder probar cortes.
- **Idioma.** Ver la sección [Nombres](#nombres). En resumen: código y comentarios en inglés, términos de dominio en español, todo lo que ve el usuario en español de México. Al cliente final se le llama **invitado**.
- **Pruebas.** Cada regla de `docs/03-reglas-de-negocio.md` tiene al menos una prueba unitaria en el API, con los ejemplos que trae el documento (por ejemplo: cambio el día 2 a las 12:01 → nueva fecha día 4).
- **Migraciones.** Solo con EF Core: `dotnet ef migrations add <Name> --project database/migrations/CleanCom.Migrations.csproj --startup-project src/api/CleanCom.Api`. No edites a mano los archivos de `database/migrations`.
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
├── CleanCom.Api/            Controladores, autenticación, SignalR hubs
├── CleanCom.Domain/         Entidades y reglas puras (sin dependencias)
├── CleanCom.Application/    Casos de uso (ScheduleRecoleccion, OptimizeRuta, ...), modelos y validadores
├── CleanCom.Infrastructure/ EF Core, Google, WhatsApp, Twilio, POS, correo, archivos
├── CleanCom.Jobs/           Procesos de Hangfire (fijas a 7 días, estatus CRM, KPIs, flota)
└── CleanCom.Tests/          xUnit
database/migrations/         Migraciones de EF Core (proyecto CleanCom.Migrations)
```

## Nombres

| Qué | Idioma | Ejemplo |
| --- | --- | --- |
| Proyectos, carpetas, clases, métodos, variables, comentarios | Inglés | `CatalogService.CreateRutaAsync`, `Models/`, `Validators/` |
| Términos de dominio de `docs/glosario.md` | Español, dentro del nombre en inglés | `Ruta`, `Zona`, `ZonaNucleoId`, `CupoParadas`, `LineaServicio`, `Operador`, `Invitado`, `ParadaType`, `RecoleccionGoal` |
| Propiedades JSON del API | Inglés en camelCase, igual que el código | `{ "code": "F", "cupoParadas": 14, "active": true }` |
| Tablas y columnas de SQL Server | `snake_case`, derivado del código por `SnakeCaseNaming` | `rutas.cupo_paradas`, `zona_visit_days`, `created_at` |
| URLs de los endpoints | Español, como están documentadas en `docs/` | `/rutas`, `/dias-inhabiles`, `/auth/renovar` |
| Datos guardados: claves de configuración, pantallas, roles | Español | `Ruta.CupoParadas`, `panel-del-dia`, `Administracion` |
| Textos al usuario: pantallas, mensajes de error y validación | Español de México | `"Ya existe la ruta F."` |
| Commits y PR | Inglés | |

En los validadores, `WithName("Nombre completo")` da el nombre en español que usan los mensajes de FluentValidation.

## Roles

`Administracion`, `Chofer` e `Invitado`. Los permisos se definen por pantalla para poder agregar roles después sin desarrollo. Ver `docs/pantallas/oficina/usuarios.md`.

## Antes de terminar una tarea

1. Compila y corre las pruebas del proyecto que tocaste.
2. Si cambiaste una regla o un endpoint, actualiza el documento correspondiente en `docs/`.
3. Si quedó algo pendiente o una duda de negocio, agrégala a `docs/09-pendientes.md` en lugar de inventar la regla.
