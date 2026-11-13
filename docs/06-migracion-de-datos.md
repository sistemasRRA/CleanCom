# Migración de datos

## Fuentes a solicitar

**Decisión (13 nov 2026): no se usa el Excel máster.** Rutas, zonas y días de visita se capturan en Operación (los días ya siguen el máster: zona 1 lunes y jueves, zona 2 martes y viernes, zona 3 miércoles y sábado). La cobertura por zona se arma en Operación › Cobertura, con el catálogo oficial de Correos de México para colonias y alcaldías. El historial de invitados y pedidos viene de la base del sistema actual.

| Fuente | Contenido esperado | Uso |
| --- | --- | --- |
| Base de datos o respaldo SQL del sistema actual | Invitados (655), pedidos, tickets, visitas, contactos del CRM, paradas fijas, promociones, mensajes, usuarios, operadores, unidades, configuración | Historial y datos vivos |
| Hoja "KPI's 2026 CleanCom" | Metas por indicador | Metas iniciales |
| POS | Tickets y facturación históricos | KPIs de venta y tickets |

**Preguntar qué base de datos usa la app actual** (está en Vercel; probablemente PostgreSQL o Supabase, no SQL Server). Eso define cómo se extraen los datos.

Los archivos de origen se guardan en `database/import/fuentes/`, carpeta excluida de Git porque contiene datos personales.

## Proceso

1. **Extraer** cada fuente a tablas de preparación (`stg_*`) en SQL Server, sin transformar.
2. **Transformar y validar** con un importador en .NET (`database/import`) que se puede correr varias veces sin duplicar:
   - Teléfonos normalizados a 10 dígitos; detectar invitados duplicados por teléfono.
   - Direcciones geocodificadas con Google; las que no se puedan ubicar van a una lista de pendientes.
   - Zona asignada por coordenadas; comparar contra la zona por CP y reportar diferencias.
   - Días de visita por zona: ya capturados en Operación (zona 1 lunes y jueves, zona 2 martes y viernes, zona 3 miércoles y sábado); lo que diga el sistema actual no los sobrescribe.
   - Tiendas propias dadas de alta como invitados (por ejemplo "7Wash Clza. del Hueso") pasan a tipo Tienda / uso interno.
   - Vueltas fijas ligadas por nombre ("OPERADORA", "TRASPASO K") se convierten en `VueltaFija` con pasos.
   - Estados técnicos (por ejemplo `no_realizada`) se mapean a las etapas y estados nuevos.
3. **Cargar** a las tablas finales y recalcular frecuencia y estatus del CRM.
4. **Reportar** conteos de origen contra destino por entidad y la lista de pendientes para oficina.

## Calendario

| Momento | Qué se migra |
| --- | --- |
| Semana 2 | Catálogos capturados en Operación (rutas F y K, zonas, días de visita) y catálogo oficial de códigos postales. Sin Excel máster |
| Semana 7 | Migración completa de prueba |
| Semanas 9 a 11 | Ensayos durante el piloto |
| Semana 12 | Migración final el día de la salida en vivo, con el sistema actual en solo lectura |

## Datos que no se migran

Contraseñas (cada usuario define la suya en el primer acceso) y mensajes "para mandar a mano" ya enviados.
