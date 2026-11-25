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
| `npm run api:types` | Regenera `src/shared/api/schema.d.ts` desde `http://localhost:5111/openapi/v1.json` (API corriendo) |

## Cliente del API

- `src/shared/api/schema.d.ts` se **genera** con `openapi-typescript`; no se edita a mano. Al cambiar o agregar un endpoint en el API, corre `npm run api:types` y `npm run typecheck`: si un DTO cambió, TypeScript marca cada pantalla afectada.
- `src/shared/api/client.ts`: cliente `openapi-fetch` tipado con las rutas del API. Agrega el token, renueva la sesión una sola vez cuando el API responde 401 y repite la petición. Si la renovación falla, cierra la sesión.
- `unwrap(...)` convierte la respuesta en datos o en `ApiError` con el mensaje en español del ProblemDetails, listo para TanStack Query.
- `src/shared/api/types.ts`: `Query<ruta, método>` y `Body<ruta, método>` sacan del OpenAPI el tipo del query string y del cuerpo de un endpoint.
- `src/shared/api/queryKeys.ts`: claves de consulta que comparten varias features (para refrescar entre pantallas).

Las pantallas **nunca llaman a `api` directo**: cada feature tiene su `services/<feature>Api.ts` con una función por endpoint.

```ts
// features/pedidos/services/pedidosApi.ts
export const pedidosApi = {
  /** POST /pedidos/{id}/programar-entrega */
  scheduleEntrega: (id: number, body: Body<'/pedidos/{id}/programar-entrega', 'post'>) =>
    unwrap(api.POST('/pedidos/{id}/programar-entrega', { params: { path: { id } }, body })),
};

// En un componente
const detail = useQuery({ queryKey: [...keys.pedidos, 'detalle', id], queryFn: () => pedidosApi.get(id) });
```

## Estructura

Por módulo (feature): todo lo de una pantalla vive junto.

```
src/
├── main.tsx              Arranque: estilos globales, proveedores y rutas
├── app/                  App (rutas por permiso), Layout y menú (screens.ts)
├── shared/               Lo que usan varias features; nunca importa de features/
│   ├── api/              Cliente, esquema generado, tipos Query/Body, claves compartidas, authApi, direccionesApi, tiempo real
│   ├── components/       Dialog, Field, PageHeader, QueryState, Toasts, Loader, Pagination, mapas, logo
│   ├── hooks/            useSave, useAction
│   ├── session/  theme/  Sesión y permisos; modo claro u oscuro
│   ├── utils/            Fechas (hora de Ciudad de México, "sáb 21 nov") y formatos
│   └── styles/global.css Sistema de diseño: tokens, modo claro y oscuro, elementos y componentes compartidos
└── features/<feature>/   agenda, auth, avisos, chofer, crm, home, invitados, operacion, panel,
    │                     paradasFijas, pedidos, planta, promociones, rutas, unidades, usuarios
    ├── pages/            Pantallas (lo que monta una ruta)
    ├── components/       Diálogos, formularios y piezas de esas pantallas
    ├── services/         <feature>Api.ts (llamadas al API), queryKeys.ts, queries.ts (hooks de TanStack Query)
    ├── types/            Alias de los tipos generados (Schemas['…']) y constantes de pantalla (etiquetas, colores)
    ├── hooks/            Hooks propios de la feature
    ├── <feature>.css     Estilos que solo usa esta feature
    └── index.ts          API pública: lo único que otras features y app/ pueden importar
public/brand/             Logo de cleancom.mx: original, recortado, versión clara para fondo oscuro y solo el símbolo
```

Reglas de la estructura:

- **Entre features solo por `index.ts`.** Por ejemplo, Clientes usa `InvitadoForm` de `features/invitados` y Pedidos usa `SendByHand` de `features/avisos`, siempre importando de la carpeta de la feature, nunca de un archivo interno. `shared/` no importa de ninguna feature.
- **Los tipos de respuesta no se escriben a mano**: `types/` solo tiene alias del esquema generado (`type Detail = Schemas['PedidoDetailDto']`) y constantes de pantalla.
- **Un componente por archivo** cuando es una pieza con nombre propio (diálogo, formulario, tarjeta); las piezas pequeñas que solo usa ese componente se quedan con él.
- **CSS**: lo de una sola feature va en `<feature>.css` (se importa en su `index.ts`); lo compartido, en `shared/styles/global.css`. Las clases llevan prefijo de la feature (`pedido-card`, `history__item`) para no chocar.
- **Formato**: Prettier con `.prettierrc.json` (comillas simples, 150 columnas). El CSS no pasa por Prettier: se escribe en reglas de una línea.

Los colores de marca y la fuente Poppins salen de cleancom.mx: azul `#7fa3d6` / `#5c84c0` / `#3e5c8a` / `#2f4a73` y marino `#22335a`. Si cambia la marca, se ajustan los tokens de `:root` en `shared/styles/global.css` y los archivos de `public/brand/`.

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
