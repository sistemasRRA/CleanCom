using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Common;
using CleanCom.Domain.Operations;
using static CleanCom.Domain.Operations.SettingKeys;

namespace CleanCom.Tests.Operations;

/// <summary>
/// docs/03-reglas-de-negocio.md, Configuración section.
/// </summary>
public class SettingCatalogTests
{
    [Theory]
    [InlineData(AgendaCorteSameDay, "13:00")]
    [InlineData(AgendaCorteDayChange, "12:00")]
    [InlineData(AgendaCorteCancellation, "13:00")]
    [InlineData(RutaBaseDeparture, "09:00")]
    [InlineData(RutaMaxReturnTime, "18:00")]
    [InlineData(RutaMaxReturnTimeSaturday, "14:00")]
    [InlineData(TurnoMorning, "09:00-13:00")]
    [InlineData(TurnoAfternoon, "13:00-18:00")]
    [InlineData(LunchMinutes, "60")]
    [InlineData(LunchFrom, "13:00")]
    [InlineData(RutaRecoleccionGoal, "10")]
    [InlineData(RutaCupoParadas, "14")]
    [InlineData(UnidadGarmentCapacity, "140")]
    [InlineData(PlantaProcessingDays, "3")]
    [InlineData(ParadaRecoleccionMinutes, "15")]
    [InlineData(ParadaEntregaMinutes, "10")]
    [InlineData(ParadaTransferMinutes, "12")]
    [InlineData(GpsToleranceMeters, "300")]
    [InlineData(VisitPhotoRequired, "true")]
    [InlineData(VisitOrderRequired, "true")]
    [InlineData(FixedDaysAhead, "7")]
    [InlineData(CrmNewDays, "45")]
    [InlineData(CrmInactiveDays, "120")]
    [InlineData(CrmAtRiskFactor, "2")]
    [InlineData(BaseLocation, "19.3608092,-99.1968494")]
    public void Initial_values_match_the_business_rules_document(string key, string expected)
    {
        var definition = SettingCatalog.Find(key);
        Assert.NotNull(definition);
        Assert.Equal(expected, definition.InitialValue);
    }

    [Theory]
    [InlineData(RutaMaxReturnTime)]
    [InlineData(RutaMaxReturnTimeSaturday)]
    [InlineData(RutaRecoleccionGoal)]
    [InlineData(RutaCupoParadas)]
    public void Keys_marked_R_can_be_overridden_per_ruta(string key)
    {
        Assert.True(SettingCatalog.Find(key)!.PerRuta);
    }

    [Fact]
    public void Every_initial_value_is_valid_for_its_type()
    {
        foreach (var d in SettingCatalog.Definitions)
        {
            Assert.Equal(d.InitialValue, SettingValue.Normalize(d.Type, d.InitialValue));
        }
    }
}

public class SettingValueTests
{
    [Theory]
    [InlineData(SettingValueType.Time, "9:00", "09:00")]
    [InlineData(SettingValueType.Time, " 13:00 ", "13:00")]
    [InlineData(SettingValueType.TimeRange, "9:00 - 13:00", "09:00-13:00")]
    [InlineData(SettingValueType.Integer, "14", "14")]
    [InlineData(SettingValueType.Decimal, "1.5", "1.5")]
    [InlineData(SettingValueType.Boolean, "sí", "true")]
    [InlineData(SettingValueType.Boolean, "No", "false")]
    [InlineData(SettingValueType.Coordinate, "19.36, -99.19", "19.36,-99.19")]
    public void Normalizes_valid_values(SettingValueType type, string value, string expected)
    {
        Assert.Equal(expected, SettingValue.Normalize(type, value));
    }

    [Theory]
    [InlineData(SettingValueType.Time, "25:00")]
    [InlineData(SettingValueType.Time, "1 p. m.")]
    [InlineData(SettingValueType.TimeRange, "13:00-09:00")]
    [InlineData(SettingValueType.TimeRange, "09:00")]
    [InlineData(SettingValueType.Integer, "-1")]
    [InlineData(SettingValueType.Integer, "catorce")]
    [InlineData(SettingValueType.Boolean, "tal vez")]
    [InlineData(SettingValueType.Coordinate, "100,-99")]
    [InlineData(SettingValueType.Text, "   ")]
    public void Rejects_invalid_values(SettingValueType type, string value)
    {
        Assert.Throws<DomainException>(() => SettingValue.Normalize(type, value));
    }
}

public class SettingResolverTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 18, 0, 0, TimeSpan.Zero);

    private static Setting Row(int id, string value, DateTimeOffset from, string key = RutaCupoParadas) =>
        new() { Id = id, Key = key, Value = value, Type = SettingValueType.Integer, EffectiveFrom = from };

    [Fact]
    public void The_latest_change_already_in_effect_applies()
    {
        var history = new[]
        {
            Row(1, "14", Now.AddDays(-30)),
            Row(2, "16", Now.AddDays(-1)),
        };
        Assert.Equal("16", SettingResolver.Resolve(history, RutaCupoParadas, Now));
    }

    [Fact]
    public void A_scheduled_change_does_not_apply_before_its_date()
    {
        var history = new[]
        {
            Row(1, "14", Now.AddDays(-30)),
            Row(2, "12", Now.AddDays(3)),
        };
        Assert.Equal("14", SettingResolver.Resolve(history, RutaCupoParadas, Now));
        Assert.Equal("12", SettingResolver.Scheduled(history, RutaCupoParadas, Now)!.Value);
        Assert.Equal("12", SettingResolver.Resolve(history, RutaCupoParadas, Now.AddDays(3)));
    }

    [Fact]
    public void The_ruta_value_overrides_the_general_one()
    {
        var history = new[] { Row(1, "14", Now.AddDays(-30)) };
        var rutaK = new Ruta { Code = "K", CupoParadas = 12 };
        var rutaF = new Ruta { Code = "F" };

        Assert.Equal("12", SettingResolver.Resolve(history, RutaCupoParadas, Now, rutaK));
        Assert.Equal("14", SettingResolver.Resolve(history, RutaCupoParadas, Now, rutaF));
    }

    [Fact]
    public void A_key_that_is_not_per_ruta_ignores_the_ruta()
    {
        var history = new[] { Row(1, "13:00", Now.AddDays(-30), AgendaCorteSameDay) };
        var ruta = new Ruta { Code = "K", CupoParadas = 12 };
        Assert.Equal("13:00", SettingResolver.Resolve(history, AgendaCorteSameDay, Now, ruta));
    }

    [Fact]
    public void The_ruta_max_return_time_is_read_as_a_time()
    {
        var ruta = new Ruta { Code = "F", MaxReturnTimeSaturday = new TimeOnly(15, 30) };
        var value = SettingResolver.Resolve([], RutaMaxReturnTimeSaturday, Now, ruta);
        Assert.Equal(new TimeOnly(15, 30), SettingValue.AsTime(value));
    }

    [Fact]
    public void An_unknown_key_is_an_error()
    {
        Assert.Throws<DomainException>(() => SettingResolver.Resolve([], "Agenda.NoExiste", Now));
    }
}
