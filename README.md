# CleanCom

Sistema de logística, rutas y seguimiento de pedidos de CleanCom, tintorería a domicilio en CDMX. Reemplaza la app actual (Next.js en Vercel) con tres aplicaciones conectadas a un mismo backend:

| Aplicación | Quién la usa | Tecnología | Carpeta |
| --- | --- | --- | --- |
| Web de oficina | Administración (logística, planta, gerencia) | React + Vite + TypeScript | `src/web` |
| App del chofer | Choferes, en tableta | React Native (Expo) | `src/mobile/chofer` |
| App del invitado | Clientes finales y contactos de convenios | React Native (Expo) | `src/mobile/invitado` |
| API | Las tres anteriores | ASP.NET Core (.NET 10 LTS) + SQL Server | `src/api` |

## Estructura

```
.
├── CLAUDE.md                 Instrucciones para Claude Code (leer primero)
├── docs/                     Especificación funcional y técnica
│   ├── 00-resumen.md
│   ├── 01-arquitectura.md
│   ├── 02-modelo-de-datos.md
│   ├── 03-reglas-de-negocio.md
│   ├── 04-integraciones.md
│   ├── 05-plan-de-trabajo.md
│   ├── 06-migracion-de-datos.md
│   ├── 07-despliegue-vps.md
│   ├── 08-costos.md
│   ├── 09-pendientes.md
│   ├── 10-guia-del-api.md
│   ├── glosario.md
│   └── pantallas/            Una ficha por pantalla
│       ├── oficina/
│       └── apps/
├── database/
│   ├── migraciones/          Scripts generados por EF Core (no editar a mano)
│   └── importacion/          Importadores del Excel máster y del sistema actual
└── src/
    ├── api/                  ASP.NET Core Web API
    ├── web/                  Web de oficina
    └── mobile/
        ├── chofer/
        ├── invitado/
        └── shared/           Código común de las dos apps
```

## Cómo empezar

1. Lee [`CLAUDE.md`](CLAUDE.md) y [`docs/00-resumen.md`](docs/00-resumen.md).
2. Las reglas de negocio están en [`docs/03-reglas-de-negocio.md`](docs/03-reglas-de-negocio.md). Ninguna regla se implementa en una pantalla: todas viven en el API.
3. Cada pantalla tiene su ficha en [`docs/pantallas/`](docs/pantallas/), con lo que se conserva del sistema actual y lo que mejora.
4. El orden de construcción está en [`docs/05-plan-de-trabajo.md`](docs/05-plan-de-trabajo.md).

El API ya tiene su base (semana 1): ver [`src/api/README.md`](src/api/README.md) para instalarlo y [`docs/10-guia-del-api.md`](docs/10-guia-del-api.md) para entender cómo está organizado y cómo agregar endpoints. La web y las apps todavía no tienen código.
