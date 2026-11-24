# App del chofer

Expo (React Native) + TypeScript, responsiva para tableta y celular. Especificación: `docs/pantallas/apps/chofer.md`. Cómo correrla: [../README.md](../README.md).

## Simular un recorrido

Para ver al chofer moverse en el mapa en vivo de oficina (Panel del día y Vista del chofer) sin un teléfono, con el API corriendo:

```
npm run simular -- --usuario luis.chofer                         # solo la ubicación
npm run simular -- --usuario luis.chofer --visitas               # además En camino y Llegué
npm run simular -- --usuario luis.chofer --reiniciar             # regresa a Pendiente lo que dejó la prueba
npm run simular -- --usuario luis.chofer --reiniciar --visitas   # reinicia y vuelve a recorrer
```

Inicia sesión como ese chofer (contraseña en `CHOFER_PASSWORD` o la pide), toma su ruta de hoy y manda ubicaciones a `POST /chofer/ubicacion` como lo hace la app, en línea recta de parada en parada.

- `--visitas`: al salir hacia cada parada marca En camino y al llegar marca Llegué, con los mismos endpoints de la app, así que la ruta pasa a "en ruta" en oficina. Nunca cierra visitas: las paradas quedan En sitio. Si se detiene con Ctrl+C a la mitad, a los `Gps.SinSenalMinutos` sale el aviso "Sin GPS" y el punto se ve gris. Al volver a correrlo sigue desde la primera parada pendiente. Una parada sin ubicación en el mapa (dirección capturada solo con código postal) también se visita, porque el API pide el orden asignado: el carro se queda donde está. Sin `--visitas` esas paradas se omiten.
- `--reiniciar`: regresa a Pendiente las paradas de hoy de ese chofer que estén En camino o En sitio (las cerradas no), y si una era entrega, su pedido a la etapa que tenía, con un evento "Reinicio de la simulación" en su historial. La app no tiene forma de deshacerlo, así que escribe directo en la base de desarrollo con `sqlcmd` y la cadena de conexión de `dotnet user-secrets` del API; solo funciona con un API local.
- Otras opciones: `--api` (por defecto `http://localhost:5111`), `--kmh 30`, `--rapido 10` (veces más rápido que el tiempo real), `--cada 3` (segundos entre lecturas) y `--pausa 15` (segundos en cada parada).

Las lecturas y las acciones se guardan en la base como las reales.
