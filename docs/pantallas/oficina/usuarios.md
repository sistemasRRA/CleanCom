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

- `GET /usuarios`, `POST /usuarios`, `PUT /usuarios/{id}`, `POST /usuarios/{id}/restablecer`.
- `GET /operadores`, `PUT /operadores/{id}`.
- `GET /invitados/cuentas?estado=`.
