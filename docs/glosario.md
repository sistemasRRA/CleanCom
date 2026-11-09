# Glosario

| Término | Significado | Nombre en código |
| --- | --- | --- |
| Invitado | Cliente final o contacto de un convenio. Se usa en todas las aplicaciones en lugar de "cliente" | `Invitado` |
| Historias | Forma en que CleanCom llama a las prendas del invitado en sus mensajes | (solo texto) |
| Convenio | Cliente empresa con condiciones especiales y paradas recurrentes | `Empresa`, `TipoParada.Convenio` |
| Base | Punto de salida y regreso de las rutas (Leones) | `Lugar` tipo Base |
| Leones | La base, Calz. de los Leones 165 | |
| Tienda 7clean | Sucursal propia (Florida, Del Valle, Polanco Schiller, etc.) | `Lugar` tipo Tienda |
| Bodega EXO | Bodega de insumos | `Lugar` tipo Bodega |
| Ruta | Recorrido diario de una unidad (F, K) | `Ruta`, `RutaDia` |
| Zona | Área geográfica de una ruta (F1, F2, F3, K1, K2, K3) | `Zona` |
| Zona EXT | Extensión de una zona núcleo; sigue sus días de visita | `Zona.ZonaNucleoId` |
| Parada | Cada punto que visita una ruta | `Parada` |
| Parada fija | Parada que se repite cada semana (convenios, insumos, traspasos) | `ParadaFija` |
| Viaje / vuelta | Varias paradas que se visitan en orden y seguidas porque la unidad va cargada | `Viaje`, `VueltaFija` |
| Traspaso | Mover ropa entre tiendas | `TipoParada.Traspaso` |
| Insumos | Bobinas, ganchos, galones; se anotan como texto | `TipoParada.Insumos` |
| Turno | Mañana (09:00 a 13:00), tarde (13:00 a 18:00) o cualquier hora | `Turno` |
| Corte | Hora límite para que algo entre el mismo día | Configuración `Agenda.*` |
| Cupo | Paradas máximas por ruta al día (14) | `Ruta.CupoParadas` |
| Pedido | Lo que se recolecta a un invitado y recorre las etapas hasta entregarse | `Pedido` |
| Ticket | Comprobante del POS por línea de servicio dentro de un pedido | `Ticket` |
| Línea de servicio | Tintorería, lavandería, planchaduría, sastrería, calzado | `LineaServicio` |
| Planta | Donde se procesan las prendas y se marcan los tickets como listos | |
| Operador | Chofer de la nómina | `Operador` |
| Unidad | Vehículo de reparto | `Unidad` |
| Sello | Beneficio de Promociones Cruzadas para invitados recurrentes | `Beneficio` |
| Cuponera | Cupones de bienvenida para invitados nuevos | `Beneficio` |
| Le toca / En riesgo / Inactivo / Prospecto | Estatus del CRM | `EstatusInvitado` |
| Máster | Excel con zonas, días y catálogos de referencia | |
| POS | Punto de venta; fuente oficial de cobros y tickets | `IProveedorPos` |
