using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Excepciones;
using Licitaciones.Domain.Servicios;
using Licitaciones.UnitTests.Comun;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>
/// Niveles de aprobación parametrizables (historia H-12, sección 8.7 del enunciado).
/// </summary>
public sealed class NivelAprobacionTests
{
    private readonly RelojFalso _reloj = new();

    private List<NivelAprobacion> CrearTablaDelEnunciado() =>
    [
        NivelAprobacion.Crear(0.01m, 999_999.99m, "Encargado de área", _reloj.Ahora),
        NivelAprobacion.Crear(1_000_000m, 9_999_999.99m, "Gerencia", _reloj.Ahora),
        NivelAprobacion.Crear(10_000_000m, null, "Junta Directiva", _reloj.Ahora),
    ];

    [Theory]
    [InlineData(0.01, "Encargado de área")]
    [InlineData(500_000, "Encargado de área")]
    [InlineData(999_999.99, "Encargado de área")]
    [InlineData(1_000_000, "Gerencia")]
    [InlineData(5_000_000, "Gerencia")]
    [InlineData(9_999_999.99, "Gerencia")]
    [InlineData(10_000_000, "Junta Directiva")]
    [InlineData(999_000_000, "Junta Directiva")]
    public void Seleccionar_DevuelveElAprobadorDelRangoQueContieneElMonto(decimal monto, string aprobadorEsperado)
    {
        List<NivelAprobacion> niveles = CrearTablaDelEnunciado();

        NivelAprobacion? nivel = SelectorNivelAprobacion.Seleccionar(niveles, monto);

        Assert.Equal(aprobadorEsperado, nivel!.Aprobador);
    }

    [Fact]
    public void Seleccionar_ConMontoFueraDeTodoRango_DevuelveNulo()
    {
        List<NivelAprobacion> niveles = CrearTablaDelEnunciado();

        Assert.Null(SelectorNivelAprobacion.Seleccionar(niveles, 0.001m));
    }

    [Fact]
    public void Seleccionar_SinNivelesConfigurados_DevuelveNulo()
    {
        Assert.Null(SelectorNivelAprobacion.Seleccionar([], 500_000m));
    }

    [Fact]
    public void Contiene_IncluyeAmbosExtremosDelRango()
    {
        var nivel = NivelAprobacion.Crear(100m, 200m, "Encargado", _reloj.Ahora);

        Assert.True(nivel.Contiene(100m));
        Assert.True(nivel.Contiene(200m));
        Assert.False(nivel.Contiene(99.99m));
        Assert.False(nivel.Contiene(200.01m));
    }

    [Fact]
    public void Contiene_EnRangoAbierto_NoTieneLimiteSuperior()
    {
        var nivel = NivelAprobacion.Crear(1_000m, null, "Junta Directiva", _reloj.Ahora);

        Assert.True(nivel.EsRangoAbierto);
        Assert.True(nivel.Contiene(decimal.MaxValue));
    }

    [Fact]
    public void Crear_ConMontoMaximoMenorQueElMinimo_Rechaza()
    {
        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            NivelAprobacion.Crear(1_000m, 500m, "Encargado", _reloj.Ahora));

        Assert.Equal(CodigosError.RangoAprobacionInvalido, excepcion.Codigo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Crear_ConMontoMinimoNoPositivo_Rechaza(decimal minimo)
    {
        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            NivelAprobacion.Crear(minimo, 1_000m, "Encargado", _reloj.Ahora));

        Assert.Equal(CodigosError.RangoAprobacionInvalido, excepcion.Codigo);
    }

    [Fact]
    public void Crear_SinAprobador_Rechaza()
    {
        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            NivelAprobacion.Crear(100m, 1_000m, "   ", _reloj.Ahora));

        Assert.Equal(CodigosError.AprobadorRequerido, excepcion.Codigo);
    }

    [Fact]
    public void GarantizarRangoConsistente_ConRangoTraslapado_Rechaza()
    {
        List<NivelAprobacion> existentes = CrearTablaDelEnunciado();
        var candidato = NivelAprobacion.Crear(500_000m, 2_000_000m, "Subgerencia", _reloj.Ahora);

        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            SelectorNivelAprobacion.GarantizarRangoConsistente(existentes, candidato));

        Assert.Equal(CodigosError.RangoAprobacionTraslapado, excepcion.Codigo);
    }

    [Fact]
    public void GarantizarRangoConsistente_ConSegundoRangoAbierto_Rechaza()
    {
        List<NivelAprobacion> existentes = CrearTablaDelEnunciado();
        var candidato = NivelAprobacion.Crear(500_000_000m, null, "Asamblea", _reloj.Ahora);

        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            SelectorNivelAprobacion.GarantizarRangoConsistente(existentes, candidato));

        Assert.Equal(CodigosError.RangoAbiertoDuplicado, excepcion.Codigo);
    }

    [Fact]
    public void GarantizarRangoConsistente_ConRangoContiguoSinTraslape_EsValido()
    {
        List<NivelAprobacion> existentes =
        [
            NivelAprobacion.Crear(0.01m, 999_999.99m, "Encargado de área", _reloj.Ahora),
        ];

        var candidato = NivelAprobacion.Crear(1_000_000m, 5_000_000m, "Gerencia", _reloj.Ahora);

        SelectorNivelAprobacion.GarantizarRangoConsistente(existentes, candidato);
    }

    [Fact]
    public void GarantizarRangoConsistente_AlEditarElMismoNivel_NoSeComparaConsigoMismo()
    {
        var nivel = NivelAprobacion.Crear(1_000m, 2_000m, "Encargado", _reloj.Ahora);
        List<NivelAprobacion> existentes = [nivel];

        nivel.Actualizar(1_000m, 3_000m, "Encargado", _reloj.Ahora);

        SelectorNivelAprobacion.GarantizarRangoConsistente(existentes, nivel);
    }
}
