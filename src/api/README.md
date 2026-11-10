# API

ASP.NET Core Web API (.NET 10) con SQL Server. Especificación: `docs/01-arquitectura.md`, `docs/02-modelo-de-datos.md`, `docs/03-reglas-de-negocio.md`, `docs/04-integraciones.md`.

Cómo está organizado el código, el recorrido de una petición, la diferencia con el MVC tradicional y los pasos para agregar un endpoint: [docs/10-guia-del-api.md](../../docs/10-guia-del-api.md).

## Proyectos

| Proyecto | Contenido |
| --- | --- |
| `CleanCom.Dominio` | Entidades y reglas puras: configuración, catálogos, zonas, permisos, hora de negocio, geografía |
| `CleanCom.Aplicacion` | Casos de uso, DTOs y validaciones (FluentValidation) |
| `CleanCom.Infraestructura` | EF Core + SQL Server (`geography`), Identity, JWT, auditoría, reloj |
| `CleanCom.Trabajos` | Procesos de Hangfire (vacío por ahora) |
| `CleanCom.Api` | Controladores (`Controllers/`), autorización por pantalla, errores, OpenAPI y Swagger UI |
| `CleanCom.Pruebas` | xUnit |
| `database/migraciones` (`CleanCom.Migraciones`) | Migraciones de EF Core; no se editan a mano |

## Primera vez

Requisitos: SDK de .NET 10, `dotnet-ef` (`dotnet tool install -g dotnet-ef`) y SQL Server con una base vacía (`cleancom_dev`).

Los secretos van en `user-secrets`, nunca en `appsettings.json`. Las claves están en [.env.example](.env.example).

```bash
cd src/api/CleanCom.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=127.0.0.1;Database=cleancom_dev;User Id=...;Password=...;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:Llave" "<al menos 32 caracteres aleatorios>"
dotnet user-secrets set "Semilla:Admin:Usuario" "admin"
dotnet user-secrets set "Semilla:Admin:Contrasena" "<contraseña inicial>"
```

Al arrancar, si no hay ningún usuario de Administración, se crea el de `Semilla:Admin`. Debe cambiar su contraseña en el primer acceso.

## Comandos (desde la raíz del repositorio)

```bash
# Aplicar migraciones
dotnet ef database update --project database/migraciones/CleanCom.Migraciones.csproj --startup-project src/api/CleanCom.Api

# Nueva migración
dotnet ef migrations add <Nombre> --project database/migraciones/CleanCom.Migraciones.csproj --startup-project src/api/CleanCom.Api

# Compilar y probar
dotnet build src/api/CleanCom.slnx
dotnet test src/api/CleanCom.slnx

# Correr el API (http://localhost:5111; Swagger en /swagger, documento OpenAPI en /openapi/v1.json, solo en Development)
dotnet run --project src/api/CleanCom.Api --launch-profile http
```

## Convenciones

- Toda hora de negocio pasa por `IRelojNegocio` (Ciudad de México); en base se guarda UTC.
- Valores configurables con `IServicioConfiguracion` y las claves de `ClavesConfiguracion`; nunca fijos en código.
- Un controlador por recurso, delgado: recibe la petición y llama al servicio de `CleanCom.Aplicacion`. Las reglas no van en el controlador.
- Un tipo por archivo. En `CleanCom.Aplicacion`, cada área (`Acceso`, `Catalogos`, `Operacion`...) tiene:
  - `Modelos/`: lo que entra (`...Request`) y lo que sale (`...Dto`) del API.
  - `Validadores/`: las reglas de FluentValidation de cada request, con mensajes en español.
  - En la raíz del área, los servicios (`ServicioCatalogos`) y sus interfaces (`IServicioUsuarios`).
  - Todo el área comparte un namespace (`CleanCom.Aplicacion.Catalogos`), sin importar la carpeta.
- Las entidades (tablas) viven en `CleanCom.Dominio`.
- Todo endpoint pide sesión salvo los marcados `[AllowAnonymous]`. Los permisos se piden por pantalla con `[RequierePantalla(Pantallas.X, NivelAcceso.Editar)]`.
- En Swagger, inicia sesión con `POST /auth/login`, copia `tokenAcceso` y pégalo en **Authorize**.
- Errores como ProblemDetails en español: 400 validación o regla, 401, 403, 404, 409.
- JSON con enums como texto (`"Monday"`, `"Tienda"`).
