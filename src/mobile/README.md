# Apps móviles

Dos apps de Expo (React Native) que comparten el paquete `shared` (cliente del API, tema, componentes y sesión):

| App | Carpeta | Especificación |
| --- | --- | --- |
| Chofer | `chofer/` | [docs/pantallas/apps/chofer.md](../../docs/pantallas/apps/chofer.md) |
| Invitado | `invitado/` | [docs/pantallas/apps/invitado.md](../../docs/pantallas/apps/invitado.md) |

## Probar en un celular (Expo Go)

1. **API en la red local.** Inícialo con el perfil `red-local` (escucha en `http://0.0.0.0:5111`):
   - Visual Studio: elige `red-local` junto a ▶ e inicia **sin depurar** (Ctrl+F5). Con el depurador, cada excepción (por ejemplo, una contraseña incorrecta) pausa el API y la app termina mostrando "No hay conexión".
   - Terminal: `dotnet run --project CleanCom.Api --launch-profile red-local` desde `src/api`.
   - Comprueba desde el celular que abra `http://<IP de la PC>:5111/health` (un 401 también cuenta: hay conexión).
2. **Metro**, una terminal por app:
   ```bash
   cd src/mobile/chofer   && npx expo start
   cd src/mobile/invitado && npx expo start --port 8082
   ```
3. **En el celular:** misma red Wi-Fi que la PC, Expo Go para el SDK del proyecto, y escanea el QR.

La app toma sola la dirección del API a partir de la de Metro (`src/mobile/shared/src/config.ts`). Para apuntar a otro servidor, copia `.env.example` a `.env` en la app y llena `EXPO_PUBLIC_API_URL`.

### Problemas comunes

| Síntoma | Causa y solución |
| --- | --- |
| Expo Go: "The request timed out" | Metro no está corriendo, o el celular no llega a la PC (otra red, aislamiento de clientes en el router o firewall). Prueba abrir `http://<IP>:8081` en el navegador del celular; si no abre, usa el hotspot del celular o `npx expo start --tunnel`. |
| Metro: "You're signed in to Expo Go as …, but not signed in to Expo CLI" | Inicia sesión en la CLI con `npx expo login`, o con un token de expo.dev: `EXPO_TOKEN="..." npx expo start` (solo en la terminal, nunca en archivos). También puedes cerrar sesión en Expo Go. |
| App: "No hay conexión con el API" | El API escucha solo en `localhost` (falta el perfil `red-local`) o está pausado en el depurador. En desarrollo, la consola de Metro muestra la causa real en una línea `[api]`. |
| App: cuenta bloqueada | 5 intentos fallidos la bloquean 15 minutos. En la web, Usuarios › Restablecer contraseña la desbloquea. |

## Entrar

- **Chofer:** un usuario con rol Chofer creado en la web (Usuarios). La primera vez pide cambiar la contraseña temporal.
- **Invitado:** el acceso con celular y código llega en la semana 5. En desarrollo, la bienvenida tiene el botón "Ver la app sin cuenta".
