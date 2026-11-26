# Modelo de datos

Entidades principales y sus campos clave. Es un punto de partida en español: en el código los campos van en inglés con los términos de dominio en español (`Ruta.Code`, `Ruta.CupoParadas`), y en SQL Server en `snake_case` (`rutas.cupo_paradas`). Ver la sección Nombres de `CLAUDE.md`. Todas las tablas llevan `id`, `created_at`, `created_by`, `modified_at`, `modified_by` y, donde aplique, `active`. Nada se borra de la base: eliminar es siempre baja lógica (`active = false`), también en pedidos, paradas, días inhábiles y cobertura. La auditoría la llena el API al guardar, en UTC. Los enums se guardan como texto.

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
| `Usuario` | NombreCompleto, Usuario, Correo?, Telefono?, Rol, Activo, DebeCambiarContrasena, UltimoAcceso | ASP.NET Identity (tablas `Usuarios`, `Roles`, `UsuariosRoles`). Roles del sistema: SuperUser, Administracion, Gerencia, Logistica, Telemarketing, Chofer, Planta, Invitado; la oficina agrega más. `roles.display_name` guarda el nombre con acentos |
| `PermisoRol` | RolId, Pantalla, Nivel (Ver, Editar) | Permisos por pantalla; Editar incluye Ver. Se editan en Roles y permisos (SuperUser no tiene filas: siempre tiene todo) y el API los lee de la base en cada petición (con caché); la sesión los entrega como `pantalla:nivel` para armar el menú |
| `TokenRenovacion` | UsuarioId, TokenHash, CreadoEn, ExpiraEn, RevocadoEn? | Solo se guarda el hash; cada renovación revoca el anterior |
| `Operador` | Nombre, Telefono, RutaHabitualId, Activo, UsuarioId? | Choferes de la nómina |
| `Invitado` | Nombre, TelefonoWhatsApp, OtroTelefono, Correo, Tipo (ClienteFinal, Convenio, Empresa, TiendaInterna), ComoNosConocio (texto de una opción del catálogo `Motivo` tipo HowFoundUs), Campana, RecibeWhatsApp (sí por omisión), InfoRuteo, Observaciones, Etiquetas, EstatusCalculado, EstatusManual?, FrecuenciaFijaDias?, FrecuenciaCalculadaDias, ProximoContacto, LeTocaFecha, CuentaApp (SinApp, Invitado, Activa, Bloqueada), UsuarioId? | Ficha del CRM |
| `Empresa` | RazonSocial, RFC, DatosFacturacion, CondicionesPrecio | Para convenios; varios `Invitado` como contactos |
| `Direccion` | InvitadoId, Etiqueta, Calle, Numero, Interior, CP, Colonia, Alcaldia, Referencias, Ubicacion (`geography`), ZonaId, ZonaAsignadaManual | Varias por invitado. En código: `Address` (tabla `addresses`) |
| `PreferenciaAviso` | InvitadoId, Push, WhatsAppServicio, WhatsAppPromociones, BajaPromocionesEn? | |
| `Contacto` | InvitadoId, Canal (Llamada, WhatsApp), Resultado, Nota, VolverAContactarEl, UsuarioId | Registro de llamadas del CRM. En código: `Contact` (tabla `contacts`) |

## Operación

| Entidad | Campos clave | Notas |
| --- | --- | --- |
| `Pedido` | Folio, InvitadoId, DireccionId, Etapa, LineasServicio, PrendasAprox, Turno, HoraEspecifica?, Prioridad, NotasChofer, HoraRegistro, Origen (Oficina, App), FechaRecoleccion, FechaEntrega?, BeneficioId? | Etapas en reglas de negocio |
| `PedidoHistorial` | PedidoId, EtapaAnterior, EtapaNueva, Motivo?, UsuarioId, Fecha | Todo cambio de etapa |
| `Ticket` | Folio (del POS), PedidoId, LineaServicioId, Prendas, Estado (EnProceso, Listo), ListoEn, Monto?, Origen (Manual, Pos), Activo | Llega del POS; mientras tanto se captura a mano. Folio único entre activos |
| `ComentarioAtraso` | TicketId o PedidoId, MotivoId?, Texto, UsuarioId | Visible en Pedidos. Se guarda como evento `AtrasoPlanta` del historial del pedido |
| `RutaDia` | RutaId, Fecha, OperadorId, UnidadId, KmInicial?, KmFinal?, InicioTurno?, FinTurno?, IdLocalInicio?, IdLocalFin?, TurnoCerradoPor? (usuario de oficina), Estado | Una por ruta y fecha. El turno del chofer va aquí; en código `ShiftStartedAt`, `StartKm`, `ShiftEndedAt`, `EndKm` |
| `Parada` | RutaDiaId, Tipo (Recoleccion, Entrega, RecoleccionYEntrega, Convenio, Insumos, Traspaso, Salida, Regreso, Comida), PedidoId?, LugarId?, DireccionId?, ViajeId?, PasoViaje?, ParadaFijaId?, Turno, Ventana, Posicion, PosicionFija, HoraEstimada, EnCaminoEn?, LlegadaEn?, CerradaEn?, UbicacionCierre?, PrendasRecogidas?, NotasCierre?, IdLocalCierre?, Estado (Pendiente, EnCamino, EnSitio, Realizada, NoRealizada), MotivoNoRealizadaId?, Insumos (texto), Notas | Una parada ocupa cupo salvo Salida, Regreso y Comida |
| `Viaje` | RutaDiaId, Tipo (Traspaso, Insumos), QueSeLleva | Pasos encadenados en orden |
| `ParadaFija` | Nombre, TipoParada (ClienteFinal, Convenio, Insumos, Traspaso), QueSeHace (Recoleccion, Entrega, RecoleccionYEntrega, Traslado), RutaId, Dias (lista en una columna), Turno (sin hora específica), LugarEnRuta (Automatica, Primera, Ultima), Insumos o carga, ContactoSitio, Telefono, PrendasAprox, NotasChofer, VigenteDesde?, VigenteHasta?, Pausada, Activa, Pasos | Con un paso es parada fija; con varios es **vuelta fija** (reemplaza ligar tramos por nombre) |
| `ParadaFijaPaso` | ParadaFijaId, Orden, LugarId? o Direccion + Ubicacion (de link de Maps o coordenadas), Indicacion, Nota, Activo | Un paso quitado al editar se desactiva |
| `ExcepcionFija` | ParadaFijaId, Fecha, Motivo, Activa | Saltar una fecha |
| `Viaje` | RutaDiaId, Nombre, Carga, ParadaFijaId?, Activo | Pasos que se visitan seguidos y en orden; lo genera una vuelta fija o un "viaje de un día" |
| `Evidencia` | ParadaId, Tipo (Foto, Firma), Ruta del archivo, TipoContenido, Tamaño, TomadaEn, Ubicacion, DistanciaParadaMetros, IdLocal (único), Activa | El archivo va en disco (`Archivos:Ruta`), nunca en SQL Server. La firma se guarda como SVG que dibuja el API. En código: `Evidence` (tabla `evidence`) |
| `CobroReferencia` | ParadaId, FormaPago (Efectivo, Tarjeta, Transferencia), Monto, Activo | Referencia; el cobro oficial está en el POS. En código: `ReferencePayment` (tabla `reference_payments`) |
| `CorteCaja` | RutaDiaId (único), OperadorId, Fecha, NotasChofer, Folio? (único), CerradoEn?, CerradoPor?, NotasOficina?, ReabiertoEn?, ReabiertoPor?, MotivoReapertura?, Lineas (FormaPago, Declarado, Registrado?, Pos?) | Uno por turno; lo crea el chofer al cerrar el turno y lo cierra oficina en Evidencias y caja. Las diferencias se calculan, no se guardan. Tablas `cortes_caja` y `corte_caja_lines` |
| `UbicacionChofer` | RutaDiaId, Ubicacion, TomadaEn, PrecisionMetros? | Historial reducido (reglas en GPS del chofer); la última posición también en memoria |

## Flota

| Entidad | Campos clave | Notas |
| --- | --- | --- |
| `Unidad` | Placa, Marca, Modelo, Anio, Tipo (Furgoneta, Pickup, Trimoto, Otro), CapacidadPrendas?, KmActual, KmActualizadoEn, EstadoServicio (Activa, FueraDeServicio), ExentaVerificacion, ServicioCadaKm?, ServicioCadaMeses?, Notas, Activa | Placa única entre activas |
| `DocumentoUnidad` | UnidadId, Tipo (Seguro, TarjetaCirculacion, Verificacion, Comprobante, Otro, Foto, FotoFalla), VigenteDesde?, VigenteHasta?, Descripcion, EventoId?, Archivo?, Activo | En código: `UnidadDocument`. El archivo va en disco |
| `EventoUnidad` | UnidadId, Tipo (Servicio, Reparacion, Falla, Pago, Kilometraje), Fecha, Km?, Monto?, Descripcion, Gravedad?, ResueltaEl?, PagoRecurrenteId?, IdLocal? (único; falla desde la app del chofer), Activo; quién lo registró en `created_by` | Bitácora. En código: `UnidadEvent` |
| `PagoRecurrente` | UnidadId, Concepto, Monto, Periodicidad (Mensual, Bimestral, Trimestral, Semestral, Anual), ProximaFecha, Activo | Tenencia, crédito, póliza en pagos. En código: `RecurringPayment` |
| `ReglaAviso` | Evento (uno por evento), DiasAntes, Destinatarios, Canales | Notificaciones de flota. En código: `FleetAlertRule` |

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
| `Indicador` | Clave, Nombre, Area, Unidad, MasEsMejor | En código (`KpiCatalog`), no en tabla: la fórmula es código. La clave (`recolecciones-por-ruta-dia`) se guarda en metas y valores |
| `MetaIndicador` | IndicadorClave, RutaId? (null = toda la empresa), Periodo (Dia, Semana, Mes), Responsable (UsuarioId), Rojo?, Verde, SuperVerde?, VigenteDesde, Activo | Las metas no reescriben el pasado: un cambio es un renglón nuevo. En código: `KpiGoal` (tabla `kpi_goals`) |
| `ValorIndicador` | IndicadorClave, Periodo (Semana, Mes, Trimestre), InicioPeriodo, RutaId? (null = total), Valor, CalculadoEn | Guardado por el proceso nocturno; recalcular actualiza el renglón. En código: `KpiValue` (tabla `kpi_values`) |
| `Encuesta` | PedidoId, Enviada, Respondida, Calificacion (0 a 10), Comentario | NPS |
