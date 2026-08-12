using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Enumeraciones;
using Licitaciones.Domain.Excepciones;
using Licitaciones.UnitTests.Comun;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>
/// Reglas de la entidad <see cref="Licitacion"/> (historias H-06, H-07 y H-10).
/// </summary>
public sealed class LicitacionTests
{
    private readonly RelojFalso _reloj = new();

    private Licitacion CrearLicitacion(decimal presupuesto = 1_000_000m, int diasParaCierre = 10) =>
        Licitacion.Crear("LIC-001", "Compra de equipo", _reloj.Ahora.AddDays(diasParaCierre), presupuesto, _reloj.Ahora);

    [Fact]
    public void Crear_ConDatosValidos_NaceEnBorradorYNormalizaElCodigo()
    {
        var licitacion = Licitacion.Crear(" lic-001 ", "  Compra   de equipo ", _reloj.Ahora.AddDays(5), 500_000m, _reloj.Ahora);

        Assert.Equal(EstadoLicitacion.Borrador, licitacion.Estado);
        Assert.Equal("lic-001", licitacion.Codigo);
        Assert.Equal("LIC-001", licitacion.CodigoNormalizado);
        Assert.Equal("Compra de equipo", licitacion.Titulo);
        Assert.Equal(500_000m, licitacion.PresupuestoEstimadoCrc);
        Assert.NotEqual(Guid.Empty, licitacion.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Crear_ConPresupuestoNoPositivo_Rechaza(decimal presupuesto)
    {
        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            Licitacion.Crear("LIC-001", "Compra", _reloj.Ahora.AddDays(5), presupuesto, _reloj.Ahora));

        Assert.Equal(CodigosError.PresupuestoInvalido, excepcion.Codigo);
    }

    [Fact]
    public void Crear_ConFechaDeCierrePasada_Rechaza()
    {
        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            Licitacion.Crear("LIC-001", "Compra", _reloj.Ahora.AddDays(-1), 100_000m, _reloj.Ahora));

        Assert.Equal(CodigosError.FechaCierreInvalida, excepcion.Codigo);
    }

    [Fact]
    public void Crear_SinCodigo_Rechaza()
    {
        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            Licitacion.Crear("   ", "Compra", _reloj.Ahora.AddDays(5), 100_000m, _reloj.Ahora));

        Assert.Equal(CodigosError.CodigoLicitacionRequerido, excepcion.Codigo);
    }

    [Fact]
    public void CambiarEstado_DeBorradorAPublicada_EsPermitido()
    {
        var licitacion = CrearLicitacion();

        licitacion.CambiarEstado(EstadoLicitacion.Publicada, _reloj.Ahora);

        Assert.Equal(EstadoLicitacion.Publicada, licitacion.Estado);
    }

    [Fact]
    public void CambiarEstado_DePublicadaABorrador_NoEsPermitido()
    {
        var licitacion = CrearLicitacion();
        licitacion.CambiarEstado(EstadoLicitacion.Publicada, _reloj.Ahora);

        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            licitacion.CambiarEstado(EstadoLicitacion.Borrador, _reloj.Ahora));

        Assert.Equal(CodigosError.TransicionNoPermitida, excepcion.Codigo);
    }

    [Theory]
    [InlineData(EstadoLicitacion.Borrador)]
    [InlineData(EstadoLicitacion.Publicada)]
    public void CambiarEstado_DesdeCerrada_NoEsPermitido(EstadoLicitacion destino)
    {
        var licitacion = CrearLicitacion();
        licitacion.CambiarEstado(EstadoLicitacion.Cerrada, _reloj.Ahora);

        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            licitacion.CambiarEstado(destino, _reloj.Ahora));

        Assert.Equal(CodigosError.TransicionNoPermitida, excepcion.Codigo);
    }

    [Fact]
    public void CambiarEstado_APublicadaConFechaDeCierreAlcanzada_Rechaza()
    {
        var licitacion = CrearLicitacion(diasParaCierre: 1);
        _reloj.Avanzar(TimeSpan.FromDays(2));

        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            licitacion.CambiarEstado(EstadoLicitacion.Publicada, _reloj.Ahora));

        Assert.Equal(CodigosError.FechaCierreInvalida, excepcion.Codigo);
    }

    [Fact]
    public void EstaCerradaFuncionalmente_CuandoSeAlcanzaLaFechaDeCierre_EsVerdadAunqueSigaPublicada()
    {
        var licitacion = CrearLicitacion(diasParaCierre: 1);
        licitacion.CambiarEstado(EstadoLicitacion.Publicada, _reloj.Ahora);

        Assert.False(licitacion.EstaCerradaFuncionalmente(_reloj.Ahora));

        _reloj.Avanzar(TimeSpan.FromDays(1));

        Assert.Equal(EstadoLicitacion.Publicada, licitacion.Estado);
        Assert.True(licitacion.EstaCerradaFuncionalmente(_reloj.Ahora));
        Assert.False(licitacion.AceptaOfertas(_reloj.Ahora));
    }

    [Fact]
    public void AceptaOfertas_SoloCuandoEstaPublicadaYVigente()
    {
        var licitacion = CrearLicitacion();

        Assert.False(licitacion.AceptaOfertas(_reloj.Ahora));

        licitacion.CambiarEstado(EstadoLicitacion.Publicada, _reloj.Ahora);

        Assert.True(licitacion.AceptaOfertas(_reloj.Ahora));
    }

    [Fact]
    public void GarantizarQueAceptaOfertas_EnLicitacionNoPublicada_IndicaElMotivo()
    {
        var licitacion = CrearLicitacion();

        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            licitacion.GarantizarQueAceptaOfertas(_reloj.Ahora));

        Assert.Equal(CodigosError.OfertaLicitacionNoPublicada, excepcion.Codigo);
    }

    [Fact]
    public void GarantizarQueAceptaOfertas_TrasElVencimiento_IndicaVencimiento()
    {
        var licitacion = CrearLicitacion(diasParaCierre: 1);
        licitacion.CambiarEstado(EstadoLicitacion.Publicada, _reloj.Ahora);
        _reloj.Avanzar(TimeSpan.FromDays(1));

        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            licitacion.GarantizarQueAceptaOfertas(_reloj.Ahora));

        Assert.Equal(CodigosError.OfertaVencida, excepcion.Codigo);
    }

    [Fact]
    public void ActualizarDatos_ConPresupuestoMenorAOfertaExistente_Rechaza()
    {
        var licitacion = CrearLicitacion(presupuesto: 1_000_000m);

        var excepcion = Assert.Throws<ReglaNegocioException>(() => licitacion.ActualizarDatos(
            "LIC-001",
            "Compra de equipo",
            _reloj.Ahora.AddDays(10),
            700_000m,
            montoOfertaMayorCrc: 800_000m,
            _reloj.Ahora));

        Assert.Equal(CodigosError.PresupuestoMenorAOferta, excepcion.Codigo);
    }

    [Fact]
    public void ActualizarDatos_ConPresupuestoIgualALaOfertaMayor_EsValido()
    {
        var licitacion = CrearLicitacion(presupuesto: 1_000_000m);

        licitacion.ActualizarDatos(
            "LIC-001",
            "Compra de equipo",
            _reloj.Ahora.AddDays(10),
            800_000m,
            montoOfertaMayorCrc: 800_000m,
            _reloj.Ahora);

        Assert.Equal(800_000m, licitacion.PresupuestoEstimadoCrc);
    }

    [Fact]
    public void ActualizarDatos_EnLicitacionCerrada_Rechaza()
    {
        var licitacion = CrearLicitacion();
        licitacion.CambiarEstado(EstadoLicitacion.Cerrada, _reloj.Ahora);

        var excepcion = Assert.Throws<ReglaNegocioException>(() => licitacion.ActualizarDatos(
            "LIC-001",
            "Otro título",
            _reloj.Ahora.AddDays(10),
            900_000m,
            null,
            _reloj.Ahora));

        Assert.Equal(CodigosError.LicitacionCerrada, excepcion.Codigo);
    }

    [Fact]
    public void Eliminar_AplicaBorradoLogicoYBloqueaCambiosDeEstado()
    {
        var licitacion = CrearLicitacion();
        licitacion.Eliminar(_reloj.Ahora);

        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            licitacion.CambiarEstado(EstadoLicitacion.Publicada, _reloj.Ahora));

        Assert.True(licitacion.EstaEliminada);
        Assert.Equal(CodigosError.LicitacionEliminada, excepcion.Codigo);
    }
}
