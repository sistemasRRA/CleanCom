namespace CleanCom.Domain.Promociones;

/// <summary>
/// Initial promotions of the demo (docs/pantallas/oficina/promociones.md): the five sellos of Promociones Cruzadas and
/// one inactive cupón per línea for the Cuponera de Bienvenida, waiting for the office to capture the printed benefit.
/// Línea ids are the seeded ones (1 Tintorería, 2 Lavandería, 3 Planchaduría, 4 Sastrería, 5 Calzado).
/// </summary>
public static class PromocionDefaults
{
    public const string CuponeraPlaceholder = "Captura aquí el beneficio impreso en la cuponera.";

    public static IReadOnlyList<Promocion> Sellos() =>
    [
        Sello("Edredón o cobija al 20%", 1, DiscountType.Porcentaje, 20,
            "20% de descuento en el lavado de un edredón, cobija o prenda voluminosa. Pago de contado. No acumulable con otras promociones.",
            "Entregar un edredón, cobija o prenda voluminosa al recibir este pedido"),
        Sello("Carga extra gratis", 2, DiscountType.ArticuloGratis, 0,
            "1 kilo de lavandería gratis al traer 5 kilos o más. Pago de contado. No acumulable con otras promociones.",
            "Entregar 5 kilos o más de lavandería al recibir este pedido"),
        Sello("Docena con 15%", 3, DiscountType.Porcentaje, 15,
            "15% de descuento al planchar una docena de piezas o más. Pago de contado. No acumulable con otras promociones.",
            "Entregar una docena de piezas o más para planchar al recibir este pedido"),
        Sello("Diagnóstico + 10%", 4, DiscountType.Porcentaje, 10,
            "Revisión y cotización sin costo, más 10% de descuento en el arreglo. Pago de contado. No acumulable con otras promociones.",
            "Entregar la prenda para arreglo al recibir este pedido"),
        Sello("Sneaker sencillo a $100", 5, DiscountType.PrecioEspecial, 100,
            "Limpieza premium de un par de sneakers a $100 pesos. Pago de contado. No acumulable con otras promociones.",
            "Entregar un par de sneakers al recibir este pedido"),
    ];

    public static IReadOnlyList<Promocion> Cuponera() =>
        new[] { (1, "Tintorería"), (2, "Lavandería"), (3, "Planchaduría"), (4, "Sastrería"), (5, "Calzado") }
            .Select(l => new Promocion
            {
                Programa = PromocionPrograma.Cuponera,
                Name = $"Bienvenida · {l.Item2}",
                LineaServicioId = l.Item1,
                DiscountType = DiscountType.Porcentaje,
                Description = CuponeraPlaceholder,
                ValidityDays = 60,
                Active = false,
            })
            .ToList();

    private static Promocion Sello(string name, int linea, DiscountType type, decimal value, string description, string condition) => new()
    {
        Programa = PromocionPrograma.Sellos,
        Name = name,
        LineaServicioId = linea,
        DiscountType = type,
        Value = value,
        Description = description,
        Condition = condition,
        ValidityDays = null,
    };
}
