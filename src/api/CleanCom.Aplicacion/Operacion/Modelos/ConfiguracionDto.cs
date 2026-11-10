using CleanCom.Dominio.Operacion;

namespace CleanCom.Aplicacion.Operacion;

public record ConfiguracionDto(
    string Clave,
    string Descripcion,
    TipoValorConfiguracion Tipo,
    bool PorRuta,
    string Valor,
    DateTimeOffset VigenteDesde,
    CambioProgramadoDto? Programado);
