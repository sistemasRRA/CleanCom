# Usuarios

**Ruta actual:** `/admin` · **Rol:** Administración

Usuarios internos, operadores e invitados con cuenta en la app.

## Se conserva

- Nuevo usuario: nombre completo, rol, usuario (los choferes entran con usuario corto, por ejemplo `juan.rosales`), contraseña inicial (mínimo 8 caracteres), operador vinculado o crear uno nuevo, teléfono, ruta habitual.
- Lista: persona, rol, chofer vinculado, activo, cambio de contraseña.
- Operadores (choferes de la nómina): nombre, teléfono, ruta habitual, activo, con edición directa que se guarda sola.

## Mejora

- Roles: se mantienen Administración y Chofer, más Invitado para la app. Permisos definidos por pantalla para agregar roles después sin desarrollo.
- La contraseña inicial se cambia en el primer acceso; nadie puede volver a verla; restablecimiento por link o código.
- Verificación en dos pasos opcional para Administración.
- Bitácora de accesos y cambios.
- Sesión por turno en tabletas compartidas.

## Invitados con cuenta en la app

Dos caminos, siempre ligados a la ficha del CRM:

1. **Se registra solo:** celular + código por WhatsApp (SMS de respaldo). Si el teléfono ya existe en el CRM, se liga a esa ficha y confirma sus datos; si no, se crea un invitado nuevo con fuente "App".
2. **Oficina lo invita:** "Invitar a la app" desde la ficha manda un link por WhatsApp.

- Vista propia de invitados con app, con filtro por estado de cuenta (sin app, invitado, activa, bloqueada).
- Bloquear acceso sin borrar historial.
- Una empresa con convenio puede tener varios contactos con cuenta propia.

## Endpoints

- `POST /auth/login`, `POST /auth/renovar`, `POST /auth/cerrar`, `POST /auth/cambiar-contrasena`, `GET /auth/yo`.
- `GET /usuarios?role=`, `POST /usuarios`, `PUT /usuarios/{id}`, `POST /usuarios/{id}/restablecer` (contraseña temporal que se cambia al entrar).
- `GET /operadores?includeInactive=`, `POST /operadores`, `PUT /operadores/{id}`.
- `GET /invitados/cuentas?estado=` (pendiente, semana 5).

## Implementado (semana 1)

- Mientras el usuario no cambia su contraseña inicial o temporal, el token solo sirve para `/auth/cambiar-contrasena` y `/auth/yo`.
- 5 intentos fallidos bloquean la cuenta 15 minutos.
- Token de acceso de 30 minutos y de renovación de 14 días (configurables en `Jwt`). Reusar un token de renovación ya usado cierra todas las sesiones del usuario.
- Desactivar o cambiar el rol de un usuario cierra sus sesiones. Siempre debe quedar un usuario de Administración activo.
- Desde esta pantalla solo se crean usuarios Administracion y Chofer; los invitados se registran desde su app.
