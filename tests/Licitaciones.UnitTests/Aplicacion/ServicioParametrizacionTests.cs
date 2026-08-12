using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Entidades;
using Licitaciones.UnitTests.Comun;

namespace Licitaciones.UnitTests.Aplicacion;

/// <summary>
/// Casos de uso de niveles de aprobación (historia H-12).
/// </summary>
public sealed class ServicioNivelesAprobacionTests
{
    private readonly ContextoServicios _contexto = new();

    [Fact]
    public async Task CrearAsync_ConRangoValido_LoRegistra()
    {
        Resultado<NivelAprobacionDto> resultado = await _contexto.NivelesAprobacion.CrearAsync(
            new GuardarNivelAprobacionDto
            {
                MontoMinimoCrc = 0.01m,
                MontoMaximoCrc = 999_999.99m,
                Aprobador = "Encargado de área",
            });

        Assert.True(resultado.EsExito);
        Assert.Single(_contexto.Almacen.NivelesAprobacion);
    }

    [Fact]
    public async Task CrearAsync_ConRangoTraslapado_DevuelveError()
    {
        _contexto.SembrarNivelesDelEnunciado();

        Resultado<NivelAprobacionDto> resultado = await _contexto.NivelesAprobacion.CrearAsync(
            new GuardarNivelAprobacionDto
            {
                MontoMinimoCrc = 500_000m,
                MontoMaximoCrc = 2_000_000m,
                Aprobador = "Subgerencia",
            });

        Assert.False(resultado.EsExito);
        Assert.Equal(CodigosError.RangoAprobacionTraslapado, resultado.Error!.Codigo);
        Assert.Equal(3, _contexto.Almacen.NivelesAprobacion.Count);
    }

    [Fact]
    public async Task CrearAsync_ConSegundoRangoAbierto_DevuelveError()
    {
        _contexto.SembrarNivelesDelEnunciado();

        Resultado<NivelAprobacionDto> resultado = await _contexto.NivelesAprobacion.CrearAsync(
            new GuardarNivelAprobacionDto
            {
                MontoMinimoCrc = 900_000_000m,
                MontoMaximoCrc = null,
                Aprobador = "Asamblea",
            });

        Assert.False(resultado.EsExito);
        Assert.Equal(CodigosError.RangoAbiertoDuplicado, resultado.Error!.Codigo);
    }

    [Theory]
    [InlineData(500_000, "Encargado de área")]
    [InlineData(5_000_000, "Gerencia")]
    [InlineData(50_000_000, "Junta Directiva")]
    public async Task ObtenerAprobadorAsync_DevuelveElAprobadorDelRango(decimal monto, string esperado)
    {
        _contexto.SembrarNivelesDelEnunciado();

        string? aprobador = await _contexto.NivelesAprobacion.ObtenerAprobadorAsync(monto);

        Assert.Equal(esperado, aprobador);
    }

    [Fact]
    public async Task ObtenerAprobadorAsync_SinNivelesConfigurados_DevuelveNulo()
    {
        Assert.Null(await _contexto.NivelesAprobacion.ObtenerAprobadorAsync(500_000m));
    }

    [Fact]
    public async Task EliminarAsync_ConNivelInexistente_DevuelveNoEncontrado()
    {
        Resultado resultado = await _contexto.NivelesAprobacion.EliminarAsync(Guid.NewGuid());

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
    }
}

/// <summary>
/// Casos de uso de tipos de cambio y conversión monetaria (historias H-13 y H-14).
/// </summary>
public sealed class ServicioTiposCambioTests
{
    private readonly ContextoServicios _contexto = new();

    [Fact]
    public async Task CrearAsync_ComoActivo_DesactivaElAnterior()
    {
        TipoCambio anterior = _contexto.SembrarTipoCambioActivo(520m);

        Resultado<TipoCambioDto> resultado = await _contexto.TiposCambio.CrearAsync(new GuardarTipoCambioDto
        {
            CrcPorUsd = 535m,
            FechaVigencia = _contexto.Reloj.Ahora,
            Activo = true,
        });

        Assert.True(resultado.EsExito);
        Assert.False(anterior.Activo);
        Assert.Single(_contexto.Almacen.TiposCambio, t => t.Activo);
        Assert.Equal(1, _contexto.Almacen.TransaccionesEjecutadas);
    }

    [Fact]
    public async Task ActivarAsync_DejaSoloUnTipoDeCambioActivo()
    {
        TipoCambio primero = _contexto.SembrarTipoCambioActivo(520m);
        var segundo = TipoCambio.Crear(540m, _contexto.Reloj.Ahora, activo: false, _contexto.Reloj.Ahora);
        _contexto.Almacen.TiposCambio.Add(segundo);

        Resultado<TipoCambioDto> resultado = await _contexto.TiposCambio.ActivarAsync(segundo.Id);

        Assert.True(resultado.EsExito);
        Assert.True(segundo.Activo);
        Assert.False(primero.Activo);
    }

    [Fact]
    public async Task EliminarAsync_ConTipoDeCambioActivo_LoImpide()
    {
        TipoCambio activo = _contexto.SembrarTipoCambioActivo();

        Resultado resultado = await _contexto.TiposCambio.EliminarAsync(activo.Id);

        Assert.False(resultado.EsExito);
        Assert.Equal(CodigosError.TipoCambioActivoNoEliminable, resultado.Error!.Codigo);
        Assert.Single(_contexto.Almacen.TiposCambio);
    }

    [Fact]
    public async Task EliminarAsync_ConTipoDeCambioInactivo_LoElimina()
    {
        var inactivo = TipoCambio.Crear(510m, _contexto.Reloj.Ahora, activo: false, _contexto.Reloj.Ahora);
        _contexto.Almacen.TiposCambio.Add(inactivo);

        Resultado resultado = await _contexto.TiposCambio.EliminarAsync(inactivo.Id);

        Assert.True(resultado.EsExito);
        Assert.Empty(_contexto.Almacen.TiposCambio);
    }

    [Fact]
    public async Task CrearAsync_ConValorNoPositivo_Rechaza()
    {
        Resultado<TipoCambioDto> resultado = await _contexto.TiposCambio.CrearAsync(new GuardarTipoCambioDto
        {
            CrcPorUsd = 0m,
            FechaVigencia = _contexto.Reloj.Ahora,
            Activo = true,
        });

        Assert.False(resultado.EsExito);
        Assert.Equal(CodigosError.TipoCambioInvalido, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task ConvertirAsync_ConTipoDeCambioActivo_DevuelveElEquivalenteYSuFecha()
    {
        TipoCambio activo = _contexto.SembrarTipoCambioActivo(520m);

        Resultado<MontoConvertidoDto> resultado = await _contexto.ConversionMoneda.ConvertirAsync(1_040_000m);

        Assert.True(resultado.EsExito);
        Assert.Equal(1_040_000m, resultado.Valor!.MontoCrc);
        Assert.Equal(2_000m, resultado.Valor.MontoUsd);
        Assert.Equal(520m, resultado.Valor.CrcPorUsd);
        Assert.Equal(activo.FechaVigencia, resultado.Valor.FechaTipoCambio);
    }

    [Fact]
    public async Task ConvertirAsync_SinTipoDeCambioActivo_DevuelveConflicto()
    {
        Resultado<MontoConvertidoDto> resultado = await _contexto.ConversionMoneda.ConvertirAsync(1_000m);

        Assert.False(resultado.EsExito);
        Assert.Equal(CodigosError.TipoCambioActivoRequerido, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task ObtenerActivoAsync_SinRegistros_DevuelveNulo()
    {
        Assert.Null(await _contexto.TiposCambio.ObtenerActivoAsync());
    }
}
