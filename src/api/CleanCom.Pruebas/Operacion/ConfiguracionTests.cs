using CleanCom.Dominio.Catalogos;
using CleanCom.Dominio.Comun;
using CleanCom.Dominio.Operacion;
using static CleanCom.Dominio.Operacion.ClavesConfiguracion;

namespace CleanCom.Pruebas.Operacion;

/// <summary>
/// docs/03-reglas-de-negocio.md, sección Configuración.
/// </summary>
public class CatalogoConfiguracionTests
{
    [Theory]
    [InlineData(AgendaCorteMismoDia, "13:00")]
    [InlineData(AgendaCorteCambioDia, "12:00")]
    [InlineData(AgendaCorteCancelacion, "13:00")]
    [InlineData(RutaSalidaBase, "09:00")]
    [InlineData(RutaRegresoMaximo, "18:00")]
    [InlineData(RutaRegresoMaximoSabado, "14:00")]
    [InlineData(TurnoManana, "09:00-13:00")]
    [InlineData(TurnoTarde, "13:00-18:00")]
    [InlineData(ComidaMinutos, "60")]
    [InlineData(ComidaDesde, "13:00")]
    [InlineData(RutaMetaRecolecciones, "10")]
    [InlineData(RutaCupoParadas, "14")]
    [InlineData(UnidadCapacidadPrendas, "140")]
    [InlineData(PlantaDiasProceso, "3")]
    [InlineData(ParadaMinutosRecoleccion, "15")]
    [InlineData(ParadaMinutosEntrega, "10")]
    [InlineData(ParadaMinutosTraslado, "12")]
    [InlineData(GpsToleranciaMetros, "300")]
    [InlineData(VisitaFotoObligatoria, "true")]
    [InlineData(VisitaOrdenObligatorio, "true")]
    [InlineData(FijasDiasAnticipacion, "7")]
    [InlineData(CrmDiasNuevo, "45")]
    [InlineData(CrmDiasInactivo, "120")]
    [InlineData(CrmFactorEnRiesgo, "2")]
    [InlineData(BaseUbicacion, "19.3608092,-99.1968494")]
    public void Valores_iniciales_son_los_del_documento_de_reglas(string clave, string esperado)
    {
        var definicion = CatalogoConfiguracion.Buscar(clave);

        Assert.NotNull(definicion);
        Assert.Equal(esperado, definicion.ValorInicial);
    }

    [Theory]
    [InlineData(RutaRegresoMaximo)]
    [InlineData(RutaRegresoMaximoSabado)]
    [InlineData(RutaMetaRecolecciones)]
    [InlineData(RutaCupoParadas)]
    public void Las_claves_marcadas_R_se_pueden_sobrescribir_por_ruta(string clave)
    {
        Assert.True(CatalogoConfiguracion.Buscar(clave)!.PorRuta);
    }

    [Fact]
    public void Todos_los_valores_iniciales_son_validos_para_su_tipo()
    {
        foreach (var d in CatalogoConfiguracion.Definiciones)
        {
            Assert.Equal(d.ValorInicial, ValorConfiguracion.Normalizar(d.Tipo, d.ValorInicial));
        }
    }
}

public class ValorConfiguracionTests
{
    [Theory]
    [InlineData(TipoValorConfiguracion.Hora, "9:00", "09:00")]
    [InlineData(TipoValorConfiguracion.Hora, " 13:00 ", "13:00")]
    [InlineData(TipoValorConfiguracion.RangoHoras, "9:00 - 13:00", "09:00-13:00")]
    [InlineData(TipoValorConfiguracion.Entero, "14", "14")]
    [InlineData(TipoValorConfiguracion.Decimal, "1.5", "1.5")]
    [InlineData(TipoValorConfiguracion.Booleano, "sí", "true")]
    [InlineData(TipoValorConfiguracion.Booleano, "No", "false")]
    [InlineData(TipoValorConfiguracion.Coordenada, "19.36, -99.19", "19.36,-99.19")]
    public void Normaliza_valores_validos(TipoValorConfiguracion tipo, string valor, string esperado)
    {
        Assert.Equal(esperado, ValorConfiguracion.Normalizar(tipo, valor));
    }

    [Theory]
    [InlineData(TipoValorConfiguracion.Hora, "25:00")]
    [InlineData(TipoValorConfiguracion.Hora, "1 p. m.")]
    [InlineData(TipoValorConfiguracion.RangoHoras, "13:00-09:00")]
    [InlineData(TipoValorConfiguracion.RangoHoras, "09:00")]
    [InlineData(TipoValorConfiguracion.Entero, "-1")]
    [InlineData(TipoValorConfiguracion.Entero, "catorce")]
    [InlineData(TipoValorConfiguracion.Booleano, "tal vez")]
    [InlineData(TipoValorConfiguracion.Coordenada, "100,-99")]
    [InlineData(TipoValorConfiguracion.Texto, "   ")]
    public void Rechaza_valores_invalidos(TipoValorConfiguracion tipo, string valor)
    {
        Assert.Throws<ExcepcionDominio>(() => ValorConfiguracion.Normalizar(tipo, valor));
    }
}

public class ResolutorConfiguracionTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 2, 18, 0, 0, TimeSpan.Zero);

    private static Configuracion Registro(int id, string valor, DateTimeOffset desde, string clave = RutaCupoParadas) =>
        new() { Id = id, Clave = clave, Valor = valor, Tipo = TipoValorConfiguracion.Entero, VigenteDesde = desde };

    [Fact]
    public void Aplica_el_ultimo_cambio_que_ya_entro_en_vigor()
    {
        var historial = new[]
        {
            Registro(1, "14", Ahora.AddDays(-30)),
            Registro(2, "16", Ahora.AddDays(-1)),
        };

        Assert.Equal("16", ResolutorConfiguracion.Resolver(historial, RutaCupoParadas, Ahora));
    }

    [Fact]
    public void Un_cambio_programado_no_aplica_antes_de_su_fecha()
    {
        var historial = new[]
        {
            Registro(1, "14", Ahora.AddDays(-30)),
            Registro(2, "12", Ahora.AddDays(3)),
        };

        Assert.Equal("14", ResolutorConfiguracion.Resolver(historial, RutaCupoParadas, Ahora));
        Assert.Equal("12", ResolutorConfiguracion.Programado(historial, RutaCupoParadas, Ahora)!.Valor);
        Assert.Equal("12", ResolutorConfiguracion.Resolver(historial, RutaCupoParadas, Ahora.AddDays(3)));
    }

    [Fact]
    public void El_valor_de_la_ruta_sobrescribe_el_general()
    {
        var historial = new[] { Registro(1, "14", Ahora.AddDays(-30)) };
        var rutaK = new Ruta { Codigo = "K", CupoParadas = 12 };
        var rutaF = new Ruta { Codigo = "F" };

        Assert.Equal("12", ResolutorConfiguracion.Resolver(historial, RutaCupoParadas, Ahora, rutaK));
        Assert.Equal("14", ResolutorConfiguracion.Resolver(historial, RutaCupoParadas, Ahora, rutaF));
    }

    [Fact]
    public void Una_clave_que_no_es_por_ruta_ignora_la_ruta()
    {
        var historial = new[] { Registro(1, "13:00", Ahora.AddDays(-30), AgendaCorteMismoDia) };
        var ruta = new Ruta { Codigo = "K", CupoParadas = 12 };

        Assert.Equal("13:00", ResolutorConfiguracion.Resolver(historial, AgendaCorteMismoDia, Ahora, ruta));
    }

    [Fact]
    public void El_regreso_maximo_de_la_ruta_se_lee_como_hora()
    {
        var ruta = new Ruta { Codigo = "F", RegresoMaximoSabado = new TimeOnly(15, 30) };

        var valor = ResolutorConfiguracion.Resolver([], RutaRegresoMaximoSabado, Ahora, ruta);

        Assert.Equal(new TimeOnly(15, 30), ValorConfiguracion.ComoHora(valor));
    }

    [Fact]
    public void Una_clave_desconocida_es_error()
    {
        Assert.Throws<ExcepcionDominio>(() => ResolutorConfiguracion.Resolver([], "Agenda.NoExiste", Ahora));
    }
}
