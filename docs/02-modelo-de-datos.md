# Modelo de datos

Entidades principales y sus campos clave. Es un punto de partida en español: en el código los campos van en inglés con los términos de dominio en español (`Ruta.Code`, `Ruta.CupoParadas`), y en SQL Server en `snake_case` (`rutas.cupo_paradas`). Ver la sección Nombres de `CLAUDE.md`. Todas las tablas llevan `id`, `created_at`, `created_by`, `modified_at`, `modified_by` y, donde aplique, `active` (baja lógica). La auditoría la llena el API al guardar, en UTC. Los enums se guardan como texto.

## Catálogos y configuración

| Entidad | Campos clave | Notas |
| --- | --- | --- |
| `Configuracion` | Clave, Valor, Tipo, VigenteDesde, ModificadoPor | Cortes, horarios, cupos, minutos por parada, umbrales. Historial de cambios y cambios programados. Ver reglas, sección Configuración |
| `Ruta` | Codigo (F, K), Nombre, Color, CupoParadas?, MetaRecolecciones?, RegresoMaximo?, RegresoMaximoSabado?, Activo | Los valores opcionales sobrescriben la configuración general |
| `Zona` | Codigo (F1, K3, K1EXT), RutaId, ZonaNucleoId?, Poligono (`geography`), Activo | Las EXT siguen los días de su zona núcleo; el núcleo es de la misma ruta y no es EXT |
| `ZonaDiaVisita` | ZonaId, DiaSemana | Lunes a domingo |
| `CoberturaCP` | CodigoPostal, Colonia, Alcaldia, ZonaId | 156 registros iniciales; base para dibujar los polígonos |
| `Lugar` | Nombre, Tipo (Base, Bodega, Tienda), Direccion, Ubicacion (`geography`), Horario | Leones, Bodega EXO, tiendas 7clean |
| `TipoParada` | Nombre (Convenio, Insumos, Traspaso), CamposRequeridos | Editable |
| `LineaServicio` | Nombre (Tintorería, Lavandería, Planchaduría, Sastrería, Calzado), DiasProceso | |
| `DiaInhabil` | Fecha, Motivo | La agenda los salta |
| `MotivoCatalogo` | Tipo (NoEntrega, AtrasoPlanta, ResultadoContacto), Texto | |

## Personas y acceso

| Entidad | Campos clave | Notas |
| --- | --- | --- |
| `Usuario` | NombreCompleto, Usuario, Correo?, Telefono?, Rol, Activo, DebeCambiarContrasena, UltimoAcceso | ASP.NET Identity (tablas `Usuarios`, `Roles`, `UsuariosRoles`). Roles: Administracion, Chofer, Invitado |
| `PermisoRol` | RolId, Pantalla, Nivel (Ver, Editar) | Permisos por pantalla; Editar incluye Ver. Viajan en el token como `pantalla:nivel` |
| `TokenRenovacion` | UsuarioId, TokenHash, CreadoEn, ExpiraEn, RevocadoEn? | Solo se guarda el hash; cada renovación revoca el anterior |
| `Operador` | Nombre, Telefono, RutaHabitualId, Activo, UsuarioId? | Choferes de la nómina |
| `Invitado` | Nombre, TelefonoWhatsApp, OtroTelefono, Correo, Tipo (ClienteFinal, Empresa, TiendaInterna), ComoNosConocio, Campana, InfoRuteo, Observaciones, Etiquetas, EstatusCalculado, EstatusManual?, FrecuenciaFijaDias?, FrecuenciaCalculadaDias, ProximoContacto, LeTocaFecha, CuentaApp (SinApp, Invitado, Activa, Bloqueada), UsuarioId? | Ficha del CRM |
| `Empresa` | RazonSocial, RFC, DatosFacturacion, CondicionesPrecio | Para convenios; varios `Invitado` como contactos |
| `Direccion` | InvitadoId, Etiqueta, Calle, Numero, Interior, CP, Colonia, Alcaldia, Referencias, Ubicacion (`geography`), ZonaId, ZonaAsignadaManual | Varias por invitado |
| `PreferenciaAviso` | InvitadoId, Push, WhatsAppServicio, WhatsAppPromociones, BajaPromocionesEn? | |
| `Contacto` | InvitadoId, Canal (Llamada, WhatsApp), Resultado, Nota, VolverAContactarEl, UsuarioId | Registro de llamadas del CRM |

## Operación

| Entidad | Campos clave | Notas |
| --- | --- | --- |
| `Pedido` | Folio, InvitadoId, DireccionId, Etapa, LineasServicio, PrendasAprox, Turno, HoraEspecifica?, Prioridad, NotasChofer, HoraRegistro, Origen (Oficina, App), FechaRecoleccion, FechaEntrega?, BeneficioId? | Etapas en reglas de negocio |
| `PedidoHistorial` | PedidoId, EtapaAnterior, EtapaNueva, Motivo?, UsuarioId, Fecha | Todo cambio de etapa |
| `Ticket` | Folio (del POS), PedidoId, LineaServicioId, Prendas, Estado (EnProceso, Listo), ListoEn, Monto? | Llega del POS |
| `ComentarioAtraso` | TicketId o PedidoId, MotivoId?, Texto, UsuarioId | Visible en Pedidos |
| `RutaDia` | RutaId, Fecha, OperadorId, UnidadId, KmInicial?, KmFinal?, InicioTurno?, FinTurno?, Estado | Una por ruta y fecha |
| `Parada` | RutaDiaId, Tipo (Recoleccion, Entrega, RecoleccionYEntrega, Convenio, Insumos, Traspaso, Salida, Regreso, Comida), PedidoId?, LugarId?, DireccionId?, ViajeId?, PasoViaje?, ParadaFijaId?, Turno, Ventana, Posicion, PosicionFija, HoraEstimada, EnCaminoEn?, LlegadaEn?, CerradaEn?, Estado (Pendiente, EnCamino, EnSitio, Realizada, NoRealizada), MotivoNoRealizadaId?, Insumos (texto), Notas | Una parada ocupa cupo salvo Salida, Regreso y Comida |
| `Viaje` | RutaDiaId, Tipo (Traspaso, Insumos), QueSeLleva | Pasos encadenados en orden |
| `ParadaFija` | Nombre, TipoParadaId, QueSeHace, RutaId, Dias, Turno, LugarEnRuta (Automatico, Primera, Ultima), LugarId?, InvitadoId?, Insumos, ContactoSitio, Telefono, PrendasAprox, Notas, VigenteDesde, VigenteHasta?, Activa | |
| `VueltaFija` | Nombre, RutaId, Dias, Turno, Pasos (orden, LugarId, QueSeHace, Nota) | Reemplaza ligar tramos por nombre |
| `ExcepcionFija` | ParadaFijaId o VueltaFijaId, Fecha, Motivo | Saltar una fecha |
| `Evidencia` | ParadaId, Tipo (Foto, Firma), Ruta del archivo, TomadaEn, Ubicacion, DistanciaParadaMetros | |
| `CobroReferencia` | ParadaId, FormaPago, Monto | Referencia; el cobro oficial está en el POS |
| `CorteCaja` | OperadorId, Fecha, Declarado (por forma de pago), Registrado, Diferencias, Folio, CerradoPor | |
| `UbicacionChofer` | RutaDiaId, Ubicacion, Fecha | Historial reducido; la última posición también en memoria |

## Flota

| Entidad | Campos clave | Notas |
| --- | --- | --- |
| `Unidad` | Placa, Marca, Modelo, Anio, CapacidadPrendas, KmActual, EstadoServicio (Activa, FueraDeServicio) | |
| `DocumentoUnidad` | UnidadId, Tipo (Seguro, Tarjeta, Verificacion), Vigencia, Archivo | |
| `EventoUnidad` | UnidadId, Tipo (Servicio, Reparacion, Falla, Pago), Fecha, Km?, Monto?, Descripcion, Gravedad?, Archivo?, ReportadoPor | Bitácora |
| `PagoRecurrente` | UnidadId, Concepto, Monto, Periodicidad, ProximaFecha | Tenencia, crédito, póliza en pagos |
| `ReglaAviso` | Evento, DiasAntes, Destinatarios, Canales | Notificaciones de flota |

## Promociones y mensajes

| Entidad | Campos clave | Notas |
| --- | --- | --- |
| `Promocion` | Programa (Sellos, Cuponera, Campana), Nombre, LineaServicioId?, TipoDescuento (Porcentaje, Monto, ArticuloGratis), Valor, Minimo?, FormaPago?, Acumulable, Condicion, Vigencia (dias o hasta entrega), Activa | |
| `Beneficio` | PromocionId, InvitadoId, Codigo (único), OtorgadoEn, VenceEn, UsadoEn?, PedidoId? | El código lo leerá el POS en fase 2 |
| `PlantillaMensaje` | Evento, NombreMeta, Categoria (Utilidad, Marketing, Autenticacion), TextoWhatsApp, TextoPush, Variables, EstadoMeta, Activa | |
| `Mensaje` | InvitadoId, PedidoId?, Canal, PlantillaId?, Texto, Estado (PorEnviar, Enviado, Entregado, Leido, Fallido, Manual), IdProveedor, Fecha | Bitácora de WhatsApp |
| `MensajeEntrante` | InvitadoId?, Telefono, Texto, Fecha, Atendido | Bandeja de respuestas |
| `Campana` | Nombre, PromocionId, Filtros (rutas, estatus), Canal, ProgramadaPara, Enviados, Canjeados | |

## KPIs

| Entidad | Campos clave | Notas |
| --- | --- | --- |
| `Indicador` | Clave, Nombre, Area, Periodo, Unidad, MasEsMejor | |
| `MetaIndicador` | IndicadorId, Responsable (UsuarioId), Rojo, Verde, SuperVerde, VigenteDesde | Las metas no reescriben el pasado |
| `ValorIndicador` | IndicadorId, Periodo, RutaId?, Valor, Calculado | Guardado por proceso programado |
| `Encuesta` | PedidoId, Enviada, Respondida, Calificacion (0 a 10), Comentario | NPS |
