using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Excepciones;
using Licitaciones.Domain.Servicios;
using Licitaciones.UnitTests.Comun;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>
/// Conversión referencial CRC/USD y administración del tipo de cambio (historias H-13 y H-14).
/// </summary>
public sealed class ConversorMonedaTests
{
    private readonly RelojFalso _reloj = new();

    [Theory]
    [InlineData(520_000, 520, 1_000.00)]
    [InlineData(1_000_000, 520, 1_923.08)]
    [InlineData(0, 520, 0)]
    [InlineData(100, 3, 33.33)]
    public void ConvertirACrcAUsd_AplicaLaFormulaDelEnunciado(decimal montoCrc, decimal crcPorUsd, decimal esperado)
    {
        decimal resultado = ConversorMoneda.ConvertirACrcAUsd(montoCrc, crcPorUsd);

        Assert.Equal(esperado, resultado);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-520)]
    public void ConvertirACrcAUsd_ConTipoDeCambioNoPositivo_Rechaza(decimal crcPorUsd)
    {
        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            ConversorMoneda.ConvertirACrcAUsd(1_000m, crcPorUsd));

        Assert.Equal(CodigosError.TipoCambioInvalido, excepcion.Codigo);
    }

    [Fact]
    public void ConvertirACrcAUsd_NoAlteraElMontoOriginal()
    {
        decimal montoCrc = 1_000_000m;

        ConversorMoneda.ConvertirACrcAUsd(montoCrc, 520m);

        Assert.Equal(1_000_000m, montoCrc);
    }

    [Fact]
    public void Crear_TipoDeCambioConValorNoPositivo_Rechaza()
    {
        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            TipoCambio.Crear(0m, _reloj.Ahora, activo: true, _reloj.Ahora));

        Assert.Equal(CodigosError.TipoCambioInvalido, excepcion.Codigo);
    }

    [Fact]
    public void ActivarYDesactivar_SonIdempotentesYAvanzanLaVersionSoloAlCambiar()
    {
        var tipoCambio = TipoCambio.Crear(520m, _reloj.Ahora, activo: false, _reloj.Ahora);

        tipoCambio.Desactivar(_reloj.Ahora);
        Assert.Equal(1, tipoCambio.Version);

        tipoCambio.Activar(_reloj.Ahora);
        Assert.True(tipoCambio.Activo);
        Assert.Equal(2, tipoCambio.Version);

        tipoCambio.Activar(_reloj.Ahora);
        Assert.Equal(2, tipoCambio.Version);
    }

    [Fact]
    public void Actualizar_RedondeaElTipoDeCambioACuatroDecimales()
    {
        var tipoCambio = TipoCambio.Crear(520m, _reloj.Ahora, activo: true, _reloj.Ahora);

        tipoCambio.Actualizar(523.456789m, _reloj.Ahora, _reloj.Ahora);

        Assert.Equal(523.4568m, tipoCambio.CrcPorUsd);
    }
}
