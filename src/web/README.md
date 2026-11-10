# Web de oficina

React 19 + Vite + TypeScript, TanStack Query y React Router. Una pantalla por ficha de `docs/pantallas/oficina/`.

## Primera vez

Requisitos: Node 24 o más reciente y **npm 11.19.1** (fijado en `package.json`; `engine-strict` impide instalar con otra versión).

```bash
cd src/web
npm install
npm run dev          # http://localhost:5173
```

El API debe estar corriendo (`dotnet run --project src/api/CleanCom.Api --launch-profile http`). En desarrollo, Vite reenvía `/api/*` a `http://localhost:5111`, así que no hace falta CORS. Copia [.env.example](.env.example) a `.env` solo si necesitas otra dirección del API.

## Comandos

| Comando | Qué hace |
| --- | --- |
| `npm run dev` | Servidor de desarrollo |
| `npm run build` | Revisa tipos y genera `dist/` para el VPS |
| `npm run typecheck` | Solo revisa tipos |
| `npm run api:types` | Regenera `src/api/schema.d.ts` desde `http://localhost:5111/openapi/v1.json` (API corriendo) |

## Cliente del API

- `src/api/schema.d.ts` se **genera** con `openapi-typescript`; no se edita a mano. Al cambiar o agregar un endpoint en el API, corre `npm run api:types` y `npm run typecheck`: si un DTO cambió, TypeScript marca cada pantalla afectada.
- `src/api/client.ts`: cliente `openapi-fetch` tipado con las rutas del API. Agrega el token, renueva la sesión una sola vez cuando el API responde 401 y repite la petición. Si la renovación falla, cierra la sesión.
- `unwrap(...)` convierte la respuesta en datos o en `ApiError` con el mensaje en español del ProblemDetails, listo para TanStack Query.
- `src/api/queries.ts`: claves de consulta compartidas y catálogos que usan varias pantallas (rutas, zonas, operadores).

```ts
const zonas = useQuery({
  queryKey: keys.zonas,
  queryFn: () => unwrap(api.GET('/zonas', { params: { query: { includeInactive: true } } })),
});
```

## Estructura

```
src/
├── api/               Cliente generado y consultas compartidas
├── components/        Diálogo, campo, estado de carga, avisos, formato de fechas
├── session/           Sesión, permisos por pantalla
├── pages/             Una carpeta por pantalla (operations/, users/)
├── screens.ts         Menú: clave de permiso, ruta y semana del plan
└── styles.css         Sistema de diseño (tokens de color, modo claro y oscuro)
```

## Reglas

- **Nombres**: código, clases CSS y comentarios en inglés; términos de dominio en español (`Ruta`, `Zona`, `cupoParadas`); textos de pantalla y rutas del navegador (`/operacion/zonas`) en español. Ver la sección Nombres de [CLAUDE.md](../../CLAUDE.md).
- **Ninguna regla de negocio en la web.** Las pantallas muestran y envían; el API valida y calcula. Por ejemplo, el polígono de una zona solo se separa en números y el API lo cierra y orienta.
- **Permisos**: el menú y los botones de editar se muestran según los permisos de `/auth/yo` (`pantalla:view` o `pantalla:edit`). Es solo presentación: el API vuelve a revisar cada petición.
- **Fechas**: el API manda UTC y la web muestra hora de Ciudad de México (`America/Mexico_City`). Las fechas de negocio (`yyyy-MM-dd`) se muestran sin convertir.
- Textos en español de México. Al cliente final se le llama invitado.

## Pantallas disponibles

| Pantalla | Qué permite |
| --- | --- |
| Inicio de sesión | Entrar; si la contraseña es inicial o temporal, obliga a cambiarla antes de continuar |
| Operación | Reglas con historial y cambios programados; rutas, zonas (días de visita, núcleo y EXT, polígono como lista de vértices), tiendas y bodegas, días inhábiles, cobertura por CP, motivos, tipos de parada y líneas de servicio |
| Usuarios | Usuarios de oficina y choferes (alta con operador, edición, restablecer contraseña) y operadores |
| Mi contraseña | Cambiar la propia contraseña |

Las demás pantallas aparecen en el menú con la semana del plan en que se construyen.

## Publicar

`npm run build` y copia `dist/` al VPS, donde el proxy la sirve en `oficina.<dominio>`. Antes, define `VITE_API_URL=https://api.<dominio>` y agrega `https://oficina.<dominio>` en `Cors__Origins__0` del API. El proxy debe devolver `index.html` para cualquier ruta (aplicación de una sola página).
