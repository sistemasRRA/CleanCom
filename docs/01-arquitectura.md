# Arquitectura

Un solo backend en C# atiende a las tres aplicaciones. Toda regla de negocio vive en el API, nunca en las pantallas.

```
            ┌──────────────┐   ┌──────────────┐   ┌──────────────┐
            │ Web oficina  │   │ App chofer   │   │ App invitado │
            │ React + Vite │   │ Expo (RN)    │   │ Expo (RN)    │
            └──────┬───────┘   └──────┬───────┘   └──────┬───────┘
                   │  HTTPS + JWT     │  + SignalR        │
                   └──────────────────┼───────────────────┘
                                      ▼
                     ┌─────────────────────────────────┐
                     │ API ASP.NET Core (.NET 10)      │
                     │ Hangfire · SignalR · Identity   │
                     └───┬───────────┬─────────────┬───┘
                         │           │             │
               ┌─────────▼──┐  ┌─────▼──────┐  ┌───▼────────────────────────┐
               │ SQL Server │  │ Disco VPS  │  │ Servicios externos         │
               │ Express    │  │ fotos/docs │  │ Google Maps · WhatsApp ·   │
               └────────────┘  └────────────┘  │ Twilio · Push · Correo · POS│
                                               └────────────────────────────┘
```

## Capas

| Capa | Tecnología | Para qué |
| --- | --- | --- |
| API | ASP.NET Core Web API (.NET 10 LTS), Entity Framework Core | Reglas de negocio y servicios para web y apps |
| Base de datos | SQL Server Express en el VPS, tipos `geography` para zonas | Operación diaria |
| Procesos programados | Hangfire (almacenado en SQL Server) | Paradas fijas a 7 días, estatus del CRM, KPIs, vencimientos de flota, envíos programados |
| Tiempo real | SignalR | Mapa en vivo, tableros que se actualizan solos, cambios de ruta al chofer |
| Archivos | Disco del VPS con respaldo externo diario | Fotos, firmas y documentos de unidades |
| Acceso | ASP.NET Identity con JWT y roles | Administración, Chofer e Invitado |
| Web de oficina | React + Vite + TypeScript | Logística, planta, gerencia y administración |
| App del chofer | React Native (Expo), responsiva para tableta y celular | Ruta, visitas, evidencias, kilometraje; offline |
| App del invitado | React Native (Expo) | Registro, pedir servicio, seguimiento, beneficios |
| Hosting | VPS propio con proxy inverso y SSL | API y web |
| Reportes (fase 2) | BigQuery | Cruce con HubSpot, pauta y nómina |

## Decisiones

- **SQL Server, no BigQuery, para la operación.** BigQuery es un almacén analítico: lento y caro para lecturas y escrituras pequeñas y sin transacciones. En fase 2 se copia la información a BigQuery cada noche para reportes.
- **Monolito modular.** Un solo API desplegable, con proyectos separados por capa (`Domain`, `Application`, `Infrastructure`, `Api`, `Jobs`). A este volumen no se justifican microservicios.
- **Transacción para el cupo.** Reservar un lugar en una ruta se hace dentro de una transacción con bloqueo por (ruta, fecha) para que dos personas no tomen el último lugar a la vez.
- **Offline en el chofer.** La app guarda en SQLite local cada acción (cambio de estado, foto, firma, kilometraje) en una cola con identificador único; el API acepta reenvíos sin duplicar (idempotencia por `IdLocal`).
- **Tiempo real con SignalR.** Grupos por rol y por ruta. La ubicación del chofer se envía cada 30 a 60 segundos durante el turno y se guarda la última posición más un historial reducido.
- **Fotos comprimidas en el dispositivo** (lado largo de 1600 px, calidad 0.7) antes de subirlas.
- **Cliente del API generado** desde OpenAPI para web y apps.

## Seguridad

- JWT de acceso de corta duración y token de renovación.
- Contraseñas con hash de Identity; la inicial se cambia en el primer acceso; nadie puede verlas.
- Verificación en dos pasos opcional para Administración.
- Bitácora de cambios (quién, cuándo, qué) en entidades clave: pedidos, rutas, configuración, usuarios, metas.
- Datos personales de invitados solo visibles para roles autorizados; los archivos de importación no se suben a Git.
- Límite de intentos en el envío de códigos de verificación por teléfono.

## Repositorio

Monorepo con API, web y apps. Ver la estructura en el [README](../README.md) y las convenciones en [CLAUDE.md](../CLAUDE.md).
