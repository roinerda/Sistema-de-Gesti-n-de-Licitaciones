using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Enumeraciones;
using Licitaciones.Domain.Excepciones;
using Licitaciones.UnitTests.Comun;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>
/// Reglas de la entidad <see cref="Oferta"/> (historias H-08 y H-09).
/// </summary>
public sealed class OfertaTests
{
    private readonly RelojFalso _reloj = new();

    private Licitacion CrearLicitacionPublicada(decimal presupuesto = 1_000_000m, int diasParaCierre = 10)
    {
        var licitacion = Licitacion.Crear(
            "LIC-001",
            "Compra de equipo",
            _reloj.Ahora.AddDays(diasParaCierre),
            presupuesto,
            _reloj.Ahora);

        licitacion.CambiarEstado(EstadoLicitacion.Publicada, _reloj.Ahora);
        return licitacion;
    }

    private Proveedor CrearProveedor() => Proveedor.Crear("Empresa Central", _reloj.Ahora);

    [Fact]
    public void Crear_ConMontoValido_RegistraLaOferta()
    {
        Licitacion licitacion = CrearLicitacionPublicada();
        Proveedor proveedor = CrearProveedor();

        var oferta = Oferta.Crear(licitacion, proveedor, 850_000m, _reloj.Ahora);

        Assert.Equal(licitacion.Id, oferta.LicitacionId);
        Assert.Equal(proveedor.Id, oferta.ProveedorId);
        Assert.Equal(850_000m, oferta.MontoOfertadoCrc);
        Assert.Equal(_reloj.Ahora, oferta.FechaRegistro);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Crear_ConMontoNoPositivo_Rechaza(decimal monto)
    {
        Licitacion licitacion = CrearLicitacionPublicada();
        Proveedor proveedor = CrearProveedor();

        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            Oferta.Crear(licitacion, proveedor, monto, _reloj.Ahora));

        Assert.Equal(CodigosError.MontoOfertaInvalido, excepcion.Codigo);
    }

    [Fact]
    public void Crear_ConMontoSuperiorAlPresupuesto_Rechaza()
    {
        Licitacion licitacion = CrearLicitacionPublicada(presupuesto: 1_000_000m);
        Proveedor proveedor = CrearProveedor();

        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            Oferta.Crear(licitacion, proveedor, 1_000_000.01m, _reloj.Ahora));

        Assert.Equal(CodigosError.OfertaSuperaPresupuesto, excepcion.Codigo);
    }

    [Fact]
    public void Crear_ConMontoIgualAlPresupuesto_EsValido()
    {
        Licitacion licitacion = CrearLicitacionPublicada(presupuesto: 1_000_000m);
        Proveedor proveedor = CrearProveedor();

        var oferta = Oferta.Crear(licitacion, proveedor, 1_000_000m, _reloj.Ahora);

        Assert.Equal(1_000_000m, oferta.MontoOfertadoCrc);
    }

    [Fact]
    public void Crear_EnLicitacionEnBorrador_Rechaza()
    {
        var licitacion = Licitacion.Crear("LIC-002", "Compra", _reloj.Ahora.AddDays(5), 500_000m, _reloj.Ahora);
        Proveedor proveedor = CrearProveedor();

        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            Oferta.Crear(licitacion, proveedor, 100_000m, _reloj.Ahora));

        Assert.Equal(CodigosError.OfertaLicitacionNoPublicada, excepcion.Codigo);
    }

    [Fact]
    public void Crear_CuandoYaSeAlcanzoLaFechaDeCierre_Rechaza()
    {
        Licitacion licitacion = CrearLicitacionPublicada(diasParaCierre: 1);
        Proveedor proveedor = CrearProveedor();
        _reloj.Avanzar(TimeSpan.FromDays(1));

        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            Oferta.Crear(licitacion, proveedor, 100_000m, _reloj.Ahora));

        Assert.Equal(CodigosError.OfertaVencida, excepcion.Codigo);
    }

    [Fact]
    public void Crear_ConProveedorEliminado_Rechaza()
    {
        Licitacion licitacion = CrearLicitacionPublicada();
        Proveedor proveedor = CrearProveedor();
        proveedor.Eliminar(_reloj.Ahora);

        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            Oferta.Crear(licitacion, proveedor, 100_000m, _reloj.Ahora));

        Assert.Equal(CodigosError.ProveedorEliminado, excepcion.Codigo);
    }

    [Fact]
    public void ActualizarMonto_EnLicitacionVigente_ActualizaMontoYVersion()
    {
        Licitacion licitacion = CrearLicitacionPublicada();
        var oferta = Oferta.Crear(licitacion, CrearProveedor(), 800_000m, _reloj.Ahora);
        _reloj.Avanzar(TimeSpan.FromHours(1));

        oferta.ActualizarMonto(licitacion, 750_000m, _reloj.Ahora);

        Assert.Equal(750_000m, oferta.MontoOfertadoCrc);
        Assert.Equal(2, oferta.Version);
    }

    [Fact]
    public void ActualizarMonto_TrasElCierreDeLaLicitacion_Rechaza()
    {
        Licitacion licitacion = CrearLicitacionPublicada();
        var oferta = Oferta.Crear(licitacion, CrearProveedor(), 800_000m, _reloj.Ahora);
        licitacion.CambiarEstado(EstadoLicitacion.Cerrada, _reloj.Ahora);

        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            oferta.ActualizarMonto(licitacion, 700_000m, _reloj.Ahora));

        Assert.Equal(CodigosError.OfertaLicitacionNoPublicada, excepcion.Codigo);
    }

    [Fact]
    public void Crear_RedondeaElMontoADosDecimales()
    {
        Licitacion licitacion = CrearLicitacionPublicada();

        var oferta = Oferta.Crear(licitacion, CrearProveedor(), 123_456.789m, _reloj.Ahora);

        Assert.Equal(123_456.79m, oferta.MontoOfertadoCrc);
    }
}
