# Guía del API

Cómo está organizado el código del API, cómo viaja una petición y cómo agregar un endpoint nuevo. Para instalar y correr el API ver [src/api/README.md](../src/api/README.md); para la arquitectura general, [01-arquitectura.md](01-arquitectura.md).

## En una frase

Es **MVC sin la V**, con la lógica fuera del controlador:

- **Controladores:** reciben la petición y regresan JSON.
- **Servicios:** aplican las reglas y guardan en la base.
- **Pantallas:** las dibujan la web (React) y las apps (Expo). El API no genera HTML.

## Proyectos

```
src/api/
├── CleanCom.Api/              Controladores, permisos, manejo de errores, Swagger
├── CleanCom.Aplicacion/       Servicios (casos de uso), modelos de entrada y salida, validadores
├── CleanCom.Dominio/          Entidades (tablas) y reglas puras, sin base de datos ni HTTP
├── CleanCom.Infraestructura/  EF Core, SQL Server, Identity, JWT, reloj; después Google, WhatsApp, POS
├── CleanCom.Trabajos/         Procesos de Hangfire (vacío por ahora)
└── CleanCom.Pruebas/          Pruebas xUnit
database/migraciones/          Migraciones de EF Core (proyecto CleanCom.Migraciones)
```

Quién puede usar a quién (las flechas no se invierten):

```
Api ──► Aplicacion ──► Dominio
 │                       ▲
 └──► Infraestructura ───┘   (Infraestructura también usa Aplicacion)
```

- `Dominio` no depende de nada. Ahí van las reglas que se prueban sin base de datos, por ejemplo qué días de visita hereda una zona EXT.
- `Aplicacion` no sabe de HTTP ni de SQL Server: trabaja con la interfaz `ICleanComDbContext`.
- Los modelos van en `Aplicacion` y no en `Api`, porque los servicios los usan y `Aplicacion` no puede depender de `Api`.

## Dónde está cada cosa

Cada área (`Acceso`, `Catalogos`, `Operacion` y las que vengan, como `Agenda` o `Pedidos`) sigue el mismo acomodo, un tipo por archivo:

```
CleanCom.Api/Controllers/RutasController.cs                       ← URL y permisos
CleanCom.Aplicacion/Catalogos/
├── Modelos/GuardarRutaRequest.cs                                 ← lo que entra
├── Modelos/RutaDto.cs                                            ← lo que sale
├── Validadores/GuardarRutaValidador.cs                           ← datos válidos
└── ServicioCatalogos.cs                                          ← reglas y guardado
CleanCom.Dominio/Catalogos/Ruta.cs                                ← la tabla
CleanCom.Infraestructura/Persistencia/Configuraciones.cs          ← cómo se mapea la tabla (RutaConfig)
```

| Pregunta | Archivo |
| --- | --- |
| ¿Qué URL es y quién puede entrar? | `Api/Controllers/RutasController.cs` |
| ¿Qué datos llegan? | `Aplicacion/Catalogos/Modelos/GuardarRutaRequest.cs` |
| ¿Qué datos salen? | `Aplicacion/Catalogos/Modelos/RutaDto.cs` |
| ¿Los datos son válidos? | `Aplicacion/Catalogos/Validadores/GuardarRutaValidador.cs` |
| ¿Qué hace y qué reglas aplica? | `Aplicacion/Catalogos/ServicioCatalogos.cs` |
| ¿Cómo es la tabla? | `Dominio/Catalogos/Ruta.cs` y `RutaConfig` |

Los archivos de un área comparten namespace aunque estén en subcarpetas: `CleanCom.Aplicacion.Catalogos`.

## Recorrido de una petición

Ejemplo: la oficina crea la Ruta F.

1. **La web manda** `POST /rutas` con `{ "codigo": "F", "nombre": "Ruta Florida", "color": "#1E88E5" }` y el token en el encabezado `Authorization: Bearer ...`.
2. **Autenticación y permisos.** ASP.NET valida el token y revisa el atributo del controlador:
   ```csharp
   [HttpPost]
   [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
   public async Task<IActionResult> Crear(GuardarRutaRequest request, CancellationToken ct)
   ```
   Sin token responde 401. Sin permiso de editar Operación responde 403.
3. **El controlador pasa el trabajo** al servicio y no hace nada más:
   ```csharp
   var ruta = await servicio.CrearRutaAsync(request, ct);
   return Created($"/rutas/{ruta.Id}", ruta);
   ```
4. **El servicio valida** con `GuardarRutaValidador`. Si un dato está mal lanza una excepción y la respuesta es 400 con los mensajes en español.
5. **El servicio aplica las reglas.** Si ya existe una ruta F lanza `ExcepcionConflicto` y la respuesta es 409.
6. **El servicio guarda** con EF Core. El interceptor de auditoría llena `CreadoEn` y `CreadoPor` solo.
7. **Respuesta:** 201 con el `RutaDto` en JSON.

Ni el controlador ni el servicio escriben respuestas de error. `ManejadorErrores` convierte las excepciones en respuestas `ProblemDetails`:

| Excepción | Respuesta |
| --- | --- |
| `ValidationException` (FluentValidation) | 400, con `errors` por campo |
| `ExcepcionDominio` (regla de negocio) | 400 |
| `ExcepcionNoAutorizado` | 401 |
| `ExcepcionNoEncontrado` | 404 |
| `ExcepcionConflicto` | 409 |
| JSON vacío o mal formado | 400 "Revisa el formato de los datos enviados." |

## Diferencia con el MVC tradicional

En el MVC tradicional, un solo proyecto tiene `Controllers/`, `Models/` y `Views/`, y el controlador lo hace todo:

```csharp
public IActionResult Create(Ruta ruta)                     // recibe la entidad directo
{
    if (!ModelState.IsValid) return View(ruta);            // valida con [Required]
    if (_db.Rutas.Any(r => r.Codigo == ruta.Codigo)) ...   // regla de negocio
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
- **Modelos separados por seguridad.** Si un controlador recibe la entidad `Usuario`, alguien podría mandar `"rol": "Administracion"`. Con `CrearUsuarioRequest` solo entran los campos que se deciden.
- **Pruebas.** `docs/03-reglas-de-negocio.md` pide una prueba por regla, y las reglas en servicios y en `Dominio` se prueban sin levantar el API.

En un sistema pequeño, solo web y con pocas reglas, el MVC tradicional (o un solo proyecto con `Controllers`, `Models` y `Services`) es suficiente. CleanCom tiene web, dos apps, procesos programados y muchas reglas, y por eso usa capas.

## Cómo agregar un endpoint

Ejemplo: un catálogo nuevo de "Colores de prenda". Los pasos son los mismos para cualquier recurso.

1. **Entidad** en `CleanCom.Dominio/<Área>/ColorPrenda.cs`. Hereda de `Entidad` y, si se puede dar de baja, implementa `IActivable`. Las reglas que no necesitan base de datos van aquí como métodos.
2. **Mapeo a tabla** en `CleanCom.Infraestructura/Persistencia/Configuraciones.cs`: una clase `ColorPrendaConfig : IEntityTypeConfiguration<ColorPrenda>` con nombre de tabla en plural, longitudes e índices únicos.
3. **DbSet**: agrega `DbSet<ColorPrenda> ColoresPrenda` en `ICleanComDbContext` (Aplicacion) y en `CleanComDbContext` (Infraestructura).
4. **Migración**, desde la raíz del repositorio, sin editar a mano los archivos generados:
   ```bash
   dotnet ef migrations add ColoresPrenda --project database/migraciones/CleanCom.Migraciones.csproj --startup-project src/api/CleanCom.Api
   dotnet ef database update --project database/migraciones/CleanCom.Migraciones.csproj --startup-project src/api/CleanCom.Api
   ```
5. **Modelos** en `CleanCom.Aplicacion/<Área>/Modelos/`: `ColorPrendaDto.cs` (sale) y `GuardarColorPrendaRequest.cs` (entra), como `record`.
6. **Validador** en `CleanCom.Aplicacion/<Área>/Validadores/GuardarColorPrendaValidador.cs`, con mensajes en español. Se registra solo: no hay que darlo de alta.
7. **Servicio** en `CleanCom.Aplicacion/<Área>/`: valida, aplica reglas, guarda y regresa el Dto. Para errores usa `ExcepcionNoEncontrado`, `ExcepcionConflicto` o `ExcepcionDominio`; nunca regreses códigos HTTP desde el servicio. Un servicio nuevo se registra en `CleanCom.Aplicacion/DependencyInjection.cs`.
8. **Controlador** en `CleanCom.Api/Controllers/ColoresPrendaController.cs`:
   ```csharp
   [ApiController]
   [Route("colores-prenda")]
   [Tags("Catálogos")]
   public class ColoresPrendaController(ServicioCatalogos servicio) : ControllerBase
   {
       [HttpGet]
       public Task<IReadOnlyList<ColorPrendaDto>> Listar(CancellationToken ct) => servicio.ListarColoresAsync(ct);

       [HttpPost]
       [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
       [ProducesResponseType<ColorPrendaDto>(StatusCodes.Status201Created)]
       public async Task<IActionResult> Crear(GuardarColorPrendaRequest request, CancellationToken ct)
       {
           var color = await servicio.CrearColorAsync(request, ct);
           return Created($"/colores-prenda/{color.Id}", color);
       }
   }
   ```
9. **Pruebas** en `CleanCom.Pruebas/<Área>/` para cada regla nueva, con los ejemplos de `docs/03-reglas-de-negocio.md`.
10. **Documentación**: actualiza la ficha de la pantalla en `docs/pantallas/` y, si cambió una regla, `docs/03-reglas-de-negocio.md`.

Reglas que no se rompen:

- Ningún valor de negocio fijo en código (cortes, cupos, metas, minutos). Se leen con `IServicioConfiguracion` y las claves de `ClavesConfiguracion`.
- Ninguna hora con `DateTime.Now`. La hora de Ciudad de México sale de `IRelojNegocio`; en la base se guarda UTC.
- El controlador no lleva lógica. Si una pantalla necesita un cálculo, se hace en un servicio y se expone en un endpoint.
- La web y las apps no repiten reglas: llaman al API.

## Permisos

- Todo endpoint pide sesión, salvo los marcados `[AllowAnonymous]` (login, renovar y cerrar sesión).
- Los permisos son por pantalla: `[RequierePantalla(Pantallas.X, NivelAcceso.Ver | Editar)]`. Editar incluye Ver.
- Las pantallas están en `CleanCom.Dominio/Acceso/Acceso.cs` (`Pantallas`) y los permisos de cada rol en la tabla `PermisosRol`. Agregar un rol no requiere tocar código.
- Mientras el usuario no cambie su contraseña inicial, su token solo sirve en endpoints con `[Authorize(Politicas.SesionIniciada)]` (`/auth/yo` y `/auth/cambiar-contrasena`).

## Probar con Swagger

1. Corre el API: `dotnet run --project src/api/CleanCom.Api --launch-profile http`.
2. Abre http://localhost:5111/swagger.
3. Ejecuta `POST /auth/login` con usuario y contraseña. Copia el `tokenAcceso` de la respuesta.
4. Pulsa **Authorize**, pega el token y confirma. Swagger lo recuerda al recargar.
5. Si el usuario debe cambiar su contraseña, hazlo con `POST /auth/cambiar-contrasena` e inicia sesión de nuevo.

## Probar con Postman

La colección [postman/CleanCom.postman_collection.json](postman/CleanCom.postman_collection.json) recorre todo el API en orden: sesión, configuración, catálogos (incluida la regla de las zonas EXT), usuarios y cierre de sesión. Cada request trae sus pruebas.

1. Postman → Import → `docs/postman/CleanCom.postman_collection.json`.
2. En las variables de la colección llena `baseUrl`, `usuario` y `contrasena` (en "valor actual", que no se exporta). Si el usuario aún debe cambiar su contraseña inicial, llena también `contrasenaNueva`.
3. Corre en orden, uno por uno o con el Runner. Los scripts guardan tokens e IDs, y cada corrida usa un sufijo distinto para no chocar con la anterior.

Los pasos marcados "(escribe)" crean datos de prueba: úsala contra `cleancom_dev`. Desde la terminal: `postman collection run docs/postman/CleanCom.postman_collection.json --env-var "contrasena=..."`.

Swagger y el documento `/openapi/v1.json` solo se publican en Development. Del documento OpenAPI se genera el cliente tipado de la web y las apps; no se escriben a mano los tipos de las respuestas.

## Compilar, probar y publicar

Se compila la solución y se publica solo `CleanCom.Api`; los demás proyectos se copian como `.dll`.

```bash
dotnet build src/api/CleanCom.slnx
dotnet test src/api/CleanCom.slnx
dotnet publish src/api/CleanCom.Api -c Release -o publicar
```

Las migraciones se aplican aparte. Para el servidor conviene un script que se puede correr más de una vez:

```bash
dotnet ef migrations script --idempotent --project database/migraciones/CleanCom.Migraciones.csproj --startup-project src/api/CleanCom.Api -o migracion.sql
```

El despliegue en el VPS se describe en [07-despliegue-vps.md](07-despliegue-vps.md).

## Endpoints actuales

| Grupo | Ruta | Permiso |
| --- | --- | --- |
| Sesión | `POST /auth/login`, `/auth/renovar`, `/auth/cerrar` | Ninguno |
| Sesión | `POST /auth/cambiar-contrasena`, `GET /auth/yo` | Sesión, aunque falte cambiar la contraseña |
| Usuarios | `GET/POST /usuarios`, `PUT /usuarios/{id}`, `POST /usuarios/{id}/restablecer` | Usuarios: Ver para leer, Editar para cambiar |
| Usuarios | `GET/POST /operadores`, `PUT /operadores/{id}` | Usuarios |
| Configuración | `GET/PUT /configuracion`, `GET /configuracion/historial` | Operación |
| Catálogos | `/rutas`, `/zonas` (y `PUT /zonas/{id}/dias`), `/lugares`, `/tipos-parada`, `/lineas-servicio`, `/motivos`, `/dias-inhabiles`, `/cobertura` | Leer: sesión. Cambiar: Operación Editar |

El detalle de cada endpoint (campos, respuestas y errores) está en Swagger.
