namespace CleanCom.Domain.Common;

/// <summary>Names in Spanish of the logged records and fields, as the office reads the bitácora.</summary>
public static class ChangeLogLabels
{
    private static readonly Dictionary<string, string> Entities = new()
    {
        ["Invitado"] = "Invitado",
        ["Address"] = "Dirección",
        ["Pedido"] = "Pedido",
        ["Ruta"] = "Ruta",
        ["Zona"] = "Zona",
        ["KpiGoal"] = "Meta de KPI",
        ["User"] = "Usuario",
        ["UserRole"] = "Rol del usuario",
        ["RolePermission"] = "Permiso",
    };

    private static readonly Dictionary<string, string> Fields = new()
    {
        // Invitado
        ["Name"] = "Nombre",
        ["WhatsAppPhone"] = "WhatsApp",
        ["OtherPhone"] = "Otro teléfono",
        ["Email"] = "Correo",
        ["Type"] = "Tipo",
        ["HowFoundUs"] = "Cómo nos conoció",
        ["ReceivesWhatsApp"] = "WhatsApp de servicio",
        ["ReceivesPush"] = "Push en la app",
        ["ReceivesPromotions"] = "Promociones por WhatsApp",
        ["Campaign"] = "Campaña",
        ["Notes"] = "Observaciones",
        ["Active"] = "Activo",
        ["UserId"] = "Cuenta de la app",
        ["NextContactOn"] = "Próximo contacto",
        ["DoNotContact"] = "No contactar",
        ["ManualStatus"] = "Estatus manual",
        ["FixedFrequencyDays"] = "Frecuencia fija (días)",
        ["CallRequestedOn"] = "Mandado a Llamar hoy",
        ["InvitedToAppAt"] = "Invitado a la app",
        // Address
        ["InvitadoId"] = "Invitado",
        ["Label"] = "Etiqueta",
        ["Street"] = "Calle",
        ["ExteriorNumber"] = "Número exterior",
        ["InteriorNumber"] = "Número interior",
        ["PostalCode"] = "CP",
        ["Neighborhood"] = "Colonia",
        ["Borough"] = "Alcaldía",
        ["References"] = "Referencias",
        ["Location"] = "Ubicación en el mapa",
        ["ZonaId"] = "Zona",
        ["ZonaAssignedManually"] = "Zona a mano",
        // Pedido
        ["AddressId"] = "Dirección",
        ["Etapa"] = "Etapa",
        ["GarmentsApprox"] = "Prendas aproximadas",
        ["Turno"] = "Turno",
        ["SpecificTime"] = "Hora específica",
        ["Priority"] = "Prioridad",
        ["DriverNotes"] = "Notas para el chofer",
        ["RegisteredAt"] = "Hora de registro",
        ["Origin"] = "Origen",
        ["RecoleccionDate"] = "Fecha de recolección",
        ["EntregaDate"] = "Fecha de entrega",
        ["FailedAttempts"] = "Intentos fallidos",
        ["EtapaSince"] = "En la etapa desde",
        // Ruta and Zona
        ["Code"] = "Clave",
        ["Color"] = "Color",
        ["CupoParadas"] = "Cupo de paradas",
        ["RecoleccionGoal"] = "Meta de recolecciones",
        ["MaxReturnTime"] = "Regreso máximo",
        ["MaxReturnTimeSaturday"] = "Regreso máximo sábado",
        ["RutaId"] = "Ruta",
        ["ZonaNucleoId"] = "Zona núcleo",
        ["Polygon"] = "Polígono",
        // KpiGoal
        ["IndicadorKey"] = "Indicador",
        ["Period"] = "Periodo",
        ["ResponsableUserId"] = "Responsable",
        ["Red"] = "Rojo",
        ["Green"] = "Verde",
        ["SuperGreen"] = "Súper verde",
        ["ValidFrom"] = "Vigente desde",
        // User, role and permission
        ["UserName"] = "Usuario",
        ["FullName"] = "Nombre completo",
        ["PhoneNumber"] = "Teléfono",
        ["MustChangePassword"] = "Debe cambiar contraseña",
        ["RoleId"] = "Rol",
        ["Screen"] = "Pantalla",
        ["Level"] = "Nivel",
    };

    /// <summary>Fields named the same with another meaning in one kind of record.</summary>
    private static readonly Dictionary<(string Entity, string Field), string> ByEntity = new()
    {
        [("UserRole", "UserId")] = "Usuario",
    };

    public static string Entity(string type) => Entities.GetValueOrDefault(type, type);

    /// <summary>The field in Spanish; an unknown field keeps its name.</summary>
    public static string Field(string entity, string field) =>
        ByEntity.GetValueOrDefault((entity, field)) ?? Fields.GetValueOrDefault(field, field);
}
