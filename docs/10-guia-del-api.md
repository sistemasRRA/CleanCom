# Guía del API

Cómo está organizado el código del API, cómo viaja una petición y cómo agregar un endpoint nuevo. Para instalar y correr el API ver [src/api/README.md](../src/api/README.md); para la arquitectura general, [01-arquitectura.md](01-arquitectura.md).

## En una frase

Es **MVC sin la V**, con la lógica fuera del controlador:

- **Controladores:** reciben la petición y regresan JSON.
- **Servicios:** aplican las reglas y guardan en la base.
- **Pantallas:** las dibujan la web (React) y las apps (Expo). El API no genera HTML.

## Nombres

El código y sus comentarios van en inglés; los términos de dominio del [glosario](glosario.md) se quedan en español dentro del nombre (`Ruta`, `Zona`, `CupoParadas`, `LineaServicio`, `Operador`). Todo lo que ve el usuario (pantallas, mensajes de error y de validación) va en español de México. La tabla completa está en la sección Nombres de [CLAUDE.md](../CLAUDE.md).

| Capa | Ejemplo |
| --- | --- |
| Clase y método | `CatalogService.CreateRutaAsync` |
| JSON | `{ "code": "F", "name": "Ruta Florida", "cupoParadas": 14, "active": true }` |
| Tabla y columna | `rutas.cupo_paradas` (`snake_case`, lo pone `SnakeCaseNaming`) |
| URL | `POST /rutas` (en español, como en las fichas de `docs/pantallas`) |
| Mensaje | `"Ya existe la ruta F."` |

## Proyectos

```
src/api/
├── CleanCom.Api/              Controladores, permisos, manejo de errores, Swagger
├── CleanCom.Application/      Servicios (casos de uso), modelos de entrada y salida, validadores
├── CleanCom.Domain/           Entidades (tablas) y reglas puras, sin base de datos ni HTTP
├── CleanCom.Infrastructure/   EF Core, SQL Server, Identity, JWT, reloj; después Google, WhatsApp, POS
├── CleanCom.Jobs/             Procesos de Hangfire (vacío por ahora)
└── CleanCom.Tests/            Pruebas xUnit
database/migrations/           Migraciones de EF Core (proyecto CleanCom.Migrations)
```

Quién puede usar a quién (las flechas no se invierten):

```
Api ──► Application ──► Domain
 │                        ▲
 └──► Infrastructure ─────┘   (Infrastructure también usa Application)
```

- `Domain` no depende de nada. Ahí van las reglas que se prueban sin base de datos, por ejemplo qué días de visita hereda una zona EXT.
- `Application` no sabe de HTTP ni de SQL Server: trabaja con la interfaz `ICleanComDbContext`.
- Los modelos van en `Application` y no en `Api`, porque los servicios los usan y `Application` no puede depender de `Api`.

## Dónde está cada cosa

Cada área (`Access`, `Catalogs`, `Operations` y las que vengan, como `Agenda` o `Pedidos`) sigue el mismo acomodo, un tipo por archivo:

```
CleanCom.Api/Controllers/RutasController.cs                       ← URL y permisos
CleanCom.Application/Catalogs/
├── Models/SaveRutaRequest.cs                                     ← lo que entra
├── Models/RutaDto.cs                                             ← lo que sale
├── Validators/SaveRutaValidator.cs                               ← datos válidos
└── CatalogService.cs                                             ← reglas y guardado
CleanCom.Domain/Catalogs/Ruta.cs                                  ← la tabla
CleanCom.Infrastructure/Persistence/Configurations.cs             ← cómo se mapea la tabla (RutaConfig)
```

| Pregunta | Archivo |
| --- | --- |
| ¿Qué URL es y quién puede entrar? | `Api/Controllers/RutasController.cs` |
| ¿Qué datos llegan? | `Application/Catalogs/Models/SaveRutaRequest.cs` |
| ¿Qué datos salen? | `Application/Catalogs/Models/RutaDto.cs` |
| ¿Los datos son válidos? | `Application/Catalogs/Validators/SaveRutaValidator.cs` |
| ¿Qué hace y qué reglas aplica? | `Application/Catalogs/CatalogService.cs` |
| ¿Cómo es la tabla? | `Domain/Catalogs/Ruta.cs` y `RutaConfig` |

Los archivos de un área comparten namespace aunque estén en subcarpetas: `CleanCom.Application.Catalogs`.

## Recorrido de una petición

Ejemplo: la oficina crea la Ruta F.

1. **La web manda** `POST /rutas` con `{ "code": "F", "name": "Ruta Florida", "color": "#1E88E5" }` y el token en el encabezado `Authorization: Bearer ...`.
2. **Autenticación y permisos.** ASP.NET valida el token y revisa el atributo del controlador:
   ```csharp
   [HttpPost]
   [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
   public async Task<IActionResult> Create(SaveRutaRequest request, CancellationToken ct)
   ```
   Sin token responde 401. Sin permiso de editar Operación responde 403.
3. **El controlador pasa el trabajo** al servicio y no hace nada más:
   ```csharp
   var ruta = await service.CreateRutaAsync(request, ct);
   return Created($"/rutas/{ruta.Id}", ruta);
   ```
4. **El servicio valida** con `SaveRutaValidator`. Si un dato está mal lanza una excepción y la respuesta es 400 con los mensajes en español.
5. **El servicio aplica las reglas.** Si ya existe una ruta F lanza `ConflictException` y la respuesta es 409.
6. **El servicio guarda** con EF Core. El `AuditInterceptor` llena `CreatedAt` y `CreatedBy` solo.
7. **Respuesta:** 201 con el `RutaDto` en JSON.

Ni el controlador ni el servicio escriben respuestas de error. `ErrorHandler` convierte las excepciones en respuestas `ProblemDetails`:

| Excepción | Respuesta |
| --- | --- |
| `ValidationException` (FluentValidation) | 400, con `errors` por campo |
| `DomainException` (regla de negocio) | 400 |
| `UnauthorizedException` | 401 |
| `NotFoundException` | 404 |
| `ConflictException` | 409 |
| JSON vacío o mal formado | 400 "Revisa el formato de los datos enviados." |

## Diferencia con el MVC tradicional

En el MVC tradicional, un solo proyecto tiene `Controllers/`, `Models/` y `Views/`, y el controlador lo hace todo:

```csharp
public IActionResult Create(Ruta ruta)                     // recibe la entidad directo
{
    if (!ModelState.IsValid) return View(ruta);            // valida con [Required]
    if (_db.Rutas.Any(r => r.Code == ruta.Code)) ...       // regla de negocio
    _db.Rutas.Add(ruta); _db.SaveChanges();                // guarda
    return RedirectToAction("Index");                      // regresa una página HTML
}
```

| | MVC tradicional | CleanCom |
| --- | --- | --- |
| Qué regresa | Páginas HTML (Views) | JSON |
| Dónde están las reglas | En el controlador | En el servicio |
| Qué hace el controlador | Todo | Recibe y entrega |
| Models | Una clase sirve para tabla, formulario y respuesta | Tres: entidad (tabla), `Request` (entra) y `Dto` (sale) |
| Validación | `[Required]`, `[MaxLength]` en el modelo | Validador de FluentValidation aparte |
| Proyectos | Uno | Varios, por capa |

Por qué aquí no conviene el tradicional:

- **No hay Views.** La oficina es React y los choferes e invitados usan apps.
- **Varios clientes comparten reglas.** Agendar una recolección lo hacen la oficina, la app del invitado y los procesos de Hangfire. Si la regla estuviera en un controlador, Hangfire no podría usarla y habría que copiarla.
- **Modelos separados por seguridad.** Si un controlador recibe la entidad `User`, alguien podría mandar `"role": "Administracion"`. Con `CreateUserRequest` solo entran los campos que se deciden.
- **Pruebas.** `docs/03-reglas-de-negocio.md` pide una prueba por regla, y las reglas en servicios y en `Domain` se prueban sin levantar el API.

En un sistema pequeño, solo web y con pocas reglas, el MVC tradicional (o un solo proyecto con `Controllers`, `Models` y `Services`) es suficiente. CleanCom tiene web, dos apps, procesos programados y muchas reglas, y por eso usa capas.

## Cómo agregar un endpoint

Ejemplo: un catálogo nuevo de "Colores de prenda". Los pasos son los mismos para cualquier recurso.

1. **Entidad** en `CleanCom.Domain/<Area>/GarmentColor.cs`. Hereda de `Entity` y, si se puede dar de baja, implementa `IActivatable`. Las reglas que no necesitan base de datos van aquí como métodos.
2. **Mapeo a tabla** en `CleanCom.Infrastructure/Persistence/Configurations.cs`: una clase `GarmentColorConfig : IEntityTypeConfiguration<GarmentColor>` con `ToTable("GarmentColors")`, longitudes e índices únicos. `SnakeCaseNaming` lo convierte en `garment_colors`; no escribas nombres en `snake_case` a mano salvo en filtros SQL (`HasFilter("[user_id] IS NOT NULL")`).
3. **DbSet**: agrega `DbSet<GarmentColor> GarmentColors` en `ICleanComDbContext` (Application) y en `CleanComDbContext` (Infrastructure).
4. **Migración**, desde la raíz del repositorio, sin editar a mano los archivos generados:
   ```bash
   dotnet ef migrations add GarmentColors --project database/migrations/CleanCom.Migrations.csproj --startup-project src/api/CleanCom.Api
   dotnet ef database update --project database/migrations/CleanCom.Migrations.csproj --startup-project src/api/CleanCom.Api
   ```
5. **Modelos** en `CleanCom.Application/<Area>/Models/`: `GarmentColorDto.cs` (sale) y `SaveGarmentColorRequest.cs` (entra), como `record`.
6. **Validador** en `CleanCom.Application/<Area>/Validators/SaveGarmentColorValidator.cs`. Cada regla lleva `WithName("Nombre")` o un `WithMessage` en español, porque el mensaje llega al usuario. Se registra solo: no hay que darlo de alta.
7. **Servicio** en `CleanCom.Application/<Area>/`: valida, aplica reglas, guarda y regresa el Dto. Para errores usa `NotFoundException`, `ConflictException` o `DomainException` con el mensaje en español; nunca regreses códigos HTTP desde el servicio. Un servicio nuevo se registra en `CleanCom.Application/DependencyInjection.cs`.
8. **Controlador** en `CleanCom.Api/Controllers/GarmentColorsController.cs`. La URL va en español:
   ```csharp
   [ApiController]
   [Route("colores-prenda")]
   [Tags("Catálogos")]
   public class GarmentColorsController(CatalogService service) : ControllerBase
   {
       [HttpGet]
       public Task<IReadOnlyList<GarmentColorDto>> List(CancellationToken ct) => service.ListGarmentColorsAsync(ct);

       [HttpPost]
       [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
       [ProducesResponseType<GarmentColorDto>(StatusCodes.Status201Created)]
       public async Task<IActionResult> Create(SaveGarmentColorRequest request, CancellationToken ct)
       {
           var color = await service.CreateGarmentColorAsync(request, ct);
           return Created($"/colores-prenda/{color.Id}", color);
       }
   }
   ```
9. **Pruebas** en `CleanCom.Tests/<Area>/` para cada regla nueva, con los ejemplos de `docs/03-reglas-de-negocio.md`.
10. **Cliente de la web**: con el API corriendo, `npm run api:types` y `npm run typecheck` en `src/web`. TypeScript marca cada pantalla afectada si un DTO cambió.
11. **Documentación**: actualiza la ficha de la pantalla en `docs/pantallas/` y, si cambió una regla, `docs/03-reglas-de-negocio.md`.

Reglas que no se rompen:

- Ningún valor de negocio fijo en código (cortes, cupos, metas, minutos). Se leen con `ISettingService` y las claves de `SettingKeys`.
- Ninguna hora con `DateTime.Now`. La hora de Ciudad de México sale de `IBusinessClock`; en la base se guarda UTC.
- El controlador no lleva lógica. Si una pantalla necesita un cálculo, se hace en un servicio y se expone en un endpoint.
- La web y las apps no repiten reglas: llaman al API.

## Permisos

- Todo endpoint pide sesión, salvo los marcados `[AllowAnonymous]` (login, renovar y cerrar sesión).
- Los permisos son por pantalla: `[RequireScreen(Screens.X, AccessLevel.View | Edit)]`. Edit incluye View. No viajan en el token: `ScreenRequirementHandler` toma el rol del token y consulta `IPermissionStore` (caché en memoria que `RoleService` limpia al guardar), así un cambio en Roles y permisos aplica en la siguiente petición. La sesión (`/auth/login`, `/auth/yo`) los entrega como `pantalla:view` o `pantalla:edit` para que el cliente arme el menú.
- Las pantallas están en `CleanCom.Domain/Access/Access.cs` (`Screens`) y los permisos de cada rol en la tabla `role_permissions`. Agregar un rol no requiere tocar código.
- Mientras el usuario no cambie su contraseña inicial, su token solo sirve en endpoints con `[Authorize(Policies.SignedIn)]` (`/auth/yo` y `/auth/cambiar-contrasena`).

## Probar con Swagger

1. Corre el API: `dotnet run --project src/api/CleanCom.Api --launch-profile http`.
2. Abre http://localhost:5111/swagger.
3. Ejecuta `POST /auth/login` con `userName` y `password`. Copia el `accessToken` de la respuesta.
4. Pulsa **Authorize**, pega el token y confirma. Swagger lo recuerda al recargar.
5. Si el usuario debe cambiar su contraseña, hazlo con `POST /auth/cambiar-contrasena` e inicia sesión de nuevo.

## Probar con Postman

La colección [postman/CleanCom.postman_collection.json](postman/CleanCom.postman_collection.json) recorre todo el API en orden: sesión, configuración, catálogos (incluida la regla de las zonas EXT), usuarios y cierre de sesión. Cada request trae sus pruebas.

1. Postman → Import → `docs/postman/CleanCom.postman_collection.json`.
2. En las variables de la colección llena `baseUrl`, `userName` y `password` (en "valor actual", que no se exporta). Si el usuario aún debe cambiar su contraseña inicial, llena también `newPassword`.
3. Corre en orden, uno por uno o con el Runner. Los scripts guardan tokens e IDs, y cada corrida usa un sufijo distinto para no chocar con la anterior.

Los pasos marcados "(escribe)" crean datos de prueba: úsala contra `cleancom_dev`. Desde la terminal: `postman collection run docs/postman/CleanCom.postman_collection.json --env-var "password=..."`.

Swagger y el documento `/openapi/v1.json` solo se publican en Development. Del documento OpenAPI se genera el cliente tipado de la web y las apps; no se escriben a mano los tipos de las respuestas.

## Probar con la web de oficina

Con el API corriendo: `cd src/web`, `npm install` y `npm run dev`, y abre http://localhost:5173. Vite reenvía `/api/*` al API, así que no hace falta CORS en desarrollo. Detalles en [src/web/README.md](../src/web/README.md).

El JSON del API usa números estrictos (`NumberHandling = Strict`): un id enviado como texto (`"5"`) es un 400. Así el cliente generado tipa los números como `number`. En producción, la web vive en otro subdominio y su origen va en `Cors:Origins`.

## Compilar, probar y publicar

Se compila la solución y se publica solo `CleanCom.Api`; los demás proyectos se copian como `.dll`.

```bash
dotnet build src/api/CleanCom.slnx
dotnet test src/api/CleanCom.slnx
dotnet publish src/api/CleanCom.Api -c Release -o publish
```

Las migraciones se aplican aparte. Para el servidor conviene un script que se puede correr más de una vez:

```bash
dotnet ef migrations script --idempotent --project database/migrations/CleanCom.Migrations.csproj --startup-project src/api/CleanCom.Api -o migration.sql
```

El despliegue en el VPS se describe en [07-despliegue-vps.md](07-despliegue-vps.md).

## Endpoints actuales

| Grupo | Ruta | Permiso |
| --- | --- | --- |
| Sesión | `POST /auth/login`, `/auth/renovar`, `/auth/cerrar` | Ninguno |
| Sesión | `POST /auth/cambiar-contrasena`, `GET /auth/yo` | Sesión, aunque falte cambiar la contraseña |
| Usuarios | `GET/POST /usuarios` (`?role=`), `PUT /usuarios/{id}`, `POST /usuarios/{id}/restablecer` | Usuarios: View para leer, Edit para cambiar |
| Usuarios | `GET/POST /operadores` (`?includeInactive=`), `PUT /operadores/{id}` | Usuarios |
| Configuración | `GET/PUT /configuracion`, `GET /configuracion/historial` (`?key=`) | Operación |
| Catálogos | `/rutas`, `/zonas` (y `PUT /zonas/{id}/dias`), `/lugares`, `/tipos-parada`, `/lineas-servicio`, `/motivos` (`?type=`), `/dias-inhabiles` (`?from=&to=`), `/cobertura` (`?search=&zonaId=`) | Leer: sesión. Cambiar: Operación Edit |
| Agenda | `POST /agenda/disponibilidad`, `POST /agenda/agendar`, `GET /agenda/tablero` (`?from=&days=`) | Agendar: View para consultar, Edit para agendar |
| Invitados | `GET /invitados/buscar?q=`, `GET/PUT /invitados/{id}`, `POST /invitados`, `POST /invitados/{id}/direcciones`, `PUT/DELETE /invitados/{id}/direcciones/{direccionId}` | Agendar: View para leer, Edit para cambiar |
| Invitados y pedidos | `DELETE /invitados/{id}` (con sus direcciones y pedidos), `DELETE /pedidos/{id}` | Solo SuperUser y Administración (política `Administrators`, fija en código). Son baja lógica; liberan el cupo |
| Pedidos | `GET /pedidos` (`?from=&to=&invitadoId=`; sin filtros, de hoy en adelante) | Agendar: View |
| Códigos postales | `GET /codigos-postales/{cp}`, `GET /codigos-postales?search=`, `GET /codigos-postales/resumen` | Sesión |
| Códigos postales | `POST /codigos-postales/importar` (TXT oficial de Correos de México) | Solo SuperUser y Administración |
| Clientes (CRM) | `GET /invitados` (`?estatus=&ruta=&q=`), `GET /invitados/llamar-hoy`, `GET /invitados/{id}/ficha` | Clientes (CRM): View |
| Clientes (CRM) | `POST /invitados/{id}/contactos`, `PUT /invitados/{id}/seguimiento` | Clientes (CRM): Edit |
| App del chofer | `GET /chofer/ruta?fecha=` | App del chofer: View |

El motor de agenda está en `CleanCom.Domain/Agenda` (cortes y búsqueda de fecha, sin base de datos) y `CleanCom.Application/Agenda` (`AgendaService`, `ZonaLocator`). `agendar` repite el cálculo dentro de una transacción con `sp_getapplock` por ruta y fecha (`AgendaLock`), así dos personas no toman el último lugar del mismo día.

El detalle de cada endpoint (campos, respuestas y errores) está en Swagger.
