# Usuarios

**Ruta actual:** `/admin` · **Rol:** Administración

Usuarios internos, operadores e invitados con cuenta en la app.

## Se conserva

- Nuevo usuario: nombre completo, rol, usuario (los choferes entran con usuario corto, por ejemplo `juan.rosales`), contraseña inicial (mínimo 8 caracteres), operador vinculado o crear uno nuevo, teléfono, ruta habitual.
- Lista: persona, rol, chofer vinculado, activo, cambio de contraseña.
- Operadores (choferes de la nómina): nombre, teléfono, ruta habitual, activo, con edición directa que se guarda sola.

## Mejora

- Roles: SuperUser, Administración, Gerencia, Logística, Telemarketing, Chofer y Planta, más Invitado para la app. Permisos definidos por pantalla para agregar roles después sin desarrollo.
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
- `GET /usuarios?role=`, `GET /usuarios/roles`, `POST /usuarios`, `PUT /usuarios/{id}`, `POST /usuarios/{id}/restablecer` (contraseña temporal que se cambia al entrar).
- `GET /operadores?includeInactive=`, `POST /operadores`, `PUT /operadores/{id}`.
- `GET /invitados/cuentas?estado=` (pendiente, semana 5).

## Implementado (semana 1)

- Mientras el usuario no cambia su contraseña inicial o temporal, el token solo sirve para `/auth/cambiar-contrasena` y `/auth/yo`.
- 5 intentos fallidos bloquean la cuenta 15 minutos.
- Token de acceso de 30 minutos y de renovación de 14 días (configurables en `Jwt`). Reusar un token de renovación ya usado cierra todas las sesiones del usuario.
- Desactivar o cambiar el rol de un usuario cierra sus sesiones. Siempre debe quedar un usuario SuperUser activo.
- Solo un SuperUser da el rol SuperUser o edita, desactiva o restablece a otro SuperUser. `GET /usuarios/roles` da los roles que el usuario actual puede asignar.
- El usuario inicial (`Seed:Admin`) se crea como SuperUser. En bases anteriores, si no hay ningún SuperUser, ese usuario pasa a SuperUser al arrancar el API.
- Desde esta pantalla se asigna cualquier rol de oficina; los invitados se registran desde su app. Solo un usuario Chofer se liga a un operador.

## Roles y permisos

Pantalla propia del menú, en Administración (`/roles-y-permisos`, permiso `roles-y-permisos`). De inicio solo SuperUser la tiene; Administración no.

- Lista de roles con cuántos usuarios tiene cada uno. Invitado no aparece porque solo usa su app.
- Para el rol elegido, cada pantalla tiene **Sin acceso**, **Ver** o **Editar**, agrupadas como en el menú lateral, con atajo "Todo" por grupo. Incluye las apps del chofer y del invitado.
- Vista previa de cómo queda el menú del rol.
- Los cambios aplican en la siguiente petición de sus usuarios, sin volver a iniciar sesión. La web actualiza el menú al regresar a la pestaña y cada minuto.
- **SuperUser** siempre ve y edita todas las pantallas de oficina, para que el sistema nunca se quede sin quien lo administre; sus permisos no se cambian. **Administración** arranca con todas menos Roles y permisos, y sí se puede ajustar.
- **Nuevo rol**: se escribe el nombre (por ejemplo "Atención a clientes") y arranca sin acceso. La clave guardada se deriva del nombre sin acentos ni espacios (`AtencionAClientes`).
- Los roles creados por la oficina se renombran (solo cambia el nombre visible) y se borran si no tienen usuarios. Los roles del sistema no se renombran ni se borran.

Endpoints: `GET /roles`, `GET /roles/pantallas`, `POST /roles`, `PUT /roles/{id}`, `PUT /roles/{id}/permisos`, `DELETE /roles/{id}`.
