# Despliegue en el VPS

El API, la web, los procesos programados y la base de datos se alojan en el VPS que CleanCom ya tiene.

## Requisitos recomendados

| Recurso | Mínimo |
| --- | --- |
| CPU | 4 núcleos |
| RAM | 8 GB (SQL Server Express usa hasta unos 1.4 GB de caché) |
| Disco | 100 GB SSD |
| Sistema | Linux (Ubuntu 22.04 o 24.04 LTS) o Windows Server |

**Pendiente:** confirmar las características y el sistema operativo del VPS actual.

## Componentes

| Componente | Cómo corre |
| --- | --- |
| API (.NET 10) | Servicio del sistema (systemd) o contenedor Docker |
| Web de oficina | Archivos estáticos servidos por el proxy |
| Proxy inverso | Nginx o Caddy, con HTTPS de Let's Encrypt; WebSockets habilitados para SignalR |
| SQL Server Express | Servicio en el mismo VPS (o contenedor oficial) |
| Archivos | Carpeta en disco configurada en el API |
| Hangfire | Dentro del proceso del API, con su panel protegido para Administración |

## Dominios sugeridos

- `api.<dominio>`: API y SignalR.
- `oficina.<dominio>`: web de oficina.
- `s.<dominio>`: ligas cortas de seguimiento para invitados sin app.

La web se compila con `VITE_API_URL=https://api.<dominio>` y el API debe aceptar su origen en `Cors__Origins__0=https://oficina.<dominio>`. El proxy sirve `index.html` para cualquier ruta de `oficina.<dominio>`.

## Respaldos

- Base de datos: respaldo completo diario y de registro de transacciones cada hora, copiados fuera del VPS.
- Archivos: sincronización diaria fuera del VPS.
- Prueba de restauración una vez al mes.

## Despliegue automático

GitHub Actions: al hacer merge a `main` se compila, se corren pruebas, se aplican migraciones y se publica en el VPS por SSH. Las apps móviles se compilan con EAS Build de Expo.

## Monitoreo

- Verificación de disponibilidad externa (por ejemplo UptimeRobot) del API y la web.
- Registros con Serilog a archivo con rotación.
- Alerta si el disco pasa del 80% o la base de datos se acerca a los 10 GB de SQL Server Express.

## Ambientes

| Ambiente | Uso |
| --- | --- |
| Desarrollo | Local de cada desarrollador (SQL Server Express o en Docker), base `cleancom_dev` |
| Pruebas | En el VPS, base separada, para el piloto y revisión |
| Producción | En el VPS |
