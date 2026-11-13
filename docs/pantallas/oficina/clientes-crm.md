# Clientes (CRM) y ficha del invitado

**Rutas actuales:** `/logistica/clientes`, `/logistica/clientes/{id}` · **Rol:** Administración

Clasifica a cada invitado por su frecuencia real y arma la lista de a quién llamar un día antes de que la ruta pase por su zona.

## Lista: se conserva

- Indicadores por estatus: Le toca, En riesgo, Nuevo, Activo, Inactivo, Prospecto (hoy 26, 76, 18, 31, 264 y 240 de 655).
- "Llamar hoy" y "Todos"; búsqueda por nombre, colonia o teléfono; filtro por ruta; "Nuevo cliente".
- Columnas: invitado con teléfono y colonia, zona, estatus, por qué ("Pide cada 7 días; su último servicio fue hace 9"), visita sugerida; acciones WhatsApp, Ficha y Agendar.

## Lista: mejora

- Registrar el resultado de la llamada desde la misma lista, sin abrir la ficha; el invitado sale de "Llamar hoy" hasta su fecha de volver a contactar.
- Estatus calculado cada noche en el API con umbrales configurables.
- Reactivación automática: push o WhatsApp de marketing a Le toca y En riesgo (con aceptación previa).
- Filtro por tipo (cliente final, empresa, tienda interna) y por estado de cuenta de app.

## Ficha: se conserva

- Indicadores: estatus, servicios, frecuencia, le toca.
- Datos: nombre, WhatsApp, otro teléfono, calle y número, CP, colonia, link de Maps, referencias, correo, tipo, cómo nos conoció, info de ruteo, observaciones, recibe avisos por WhatsApp, cliente activo.
- Registrar contacto: canal, resultado, nota, volver a contactar el (con cálculo automático si se deja vacío).
- Seguimiento: estatus automático o manual, frecuencia fija, próximo contacto, etiquetas.
- Historia: pedidos, visitas, WhatsApp y llamadas.
- "Agendar recolección".

## Ficha: mejora

- Pestañas: Resumen, Datos, Direcciones, Pedidos, Contactos, Beneficios y, para empresas, Facturación y Contactos de la empresa.
- Varias direcciones por invitado, cada una con su zona.
- Alerta "sin zona" o "sin ubicación en el mapa".
- Un esquema de guardado por sección con bitácora de quién cambió qué.
- Historia legible y filtrable, con montos.
- Indicadores de valor: gasto total, ticket promedio, último servicio.
- Preferencias de aviso por canal: push, WhatsApp de servicio, WhatsApp de promociones.
- Detección y fusión de duplicados por teléfono.
- Tipo "Tienda / uso interno" para que las tiendas no cuenten en métricas.
- Estado de la cuenta de app (sin app, invitado, activa, bloqueada), último acceso, "Invitar a la app" y bloquear acceso.

## Endpoints

- `GET /invitados?estatus=&ruta=&q=`, `GET /invitados/llamar-hoy` (semana 2, hecho): filas con estatus, por qué, frecuencia, le toca, visita sugerida, próxima recolección y próximo contacto, más los conteos por estatus (sin tiendas) y cuántos hay en "Llamar hoy". Permiso Clientes (CRM): View.
- `GET /invitados/{id}/ficha` (hecho): datos, direcciones, evaluación del CRM, pedidos y contactos.
- `GET /invitados/{id}`, `PUT /invitados/{id}`, `POST /invitados`, `POST /invitados/{id}/fusionar`.
- `POST /invitados/{id}/contactos`, `PUT /invitados/{id}/seguimiento` (hecho; Clientes (CRM): Edit). Seguimiento: estatus manual, frecuencia fija, próximo contacto y no contactar.
- Pendiente: `POST /invitados/{id}/fusionar`, invitar o bloquear en la app, bitácora por sección e indicadores de valor (gasto, ticket promedio).
- `POST /invitados/{id}/invitar-app`, `POST /invitados/{id}/bloquear-app`.

## Criterios de aceptación

- Un contacto "no contestó" sin fecha programa el siguiente para mañana y saca al invitado de "Llamar hoy".
- Capturar un teléfono existente avisa del duplicado.

## Estado (semana 2)

- El estatus se calcula al consultar con la misma regla (`CrmRules`) que usará el proceso nocturno cuando Hangfire esté conectado; entonces se guardará por la noche junto con su "por qué".
- Un servicio es un pedido activo, no cancelado, con fecha de recolección de hoy o antes. Mientras no se importe el historial del Excel, casi todos los invitados salen como Prospecto.
- La zona del CRM es la de la primera dirección activa con zona.
