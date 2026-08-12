using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Enumeraciones;
using Licitaciones.UnitTests.Comun;

namespace Licitaciones.UnitTests.Aplicacion;

/// <summary>
/// Casos de uso de ofertas (historias H-08 y H-09).
/// </summary>
public sealed class ServicioOfertasTests
{
    private readonly ContextoServicios _contexto = new();

    [Fact]
    public async Task CrearAsync_ConDatosValidos_RegistraLaOferta()
    {
        Licitacion licitacion = _contexto.SembrarLicitacion(presupuesto: 1_000_000m);
        Proveedor proveedor = _contexto.SembrarProveedor();

        Resultado<OfertaDto> resultado = await _contexto.Ofertas.CrearAsync(new GuardarOfertaDto
        {
            LicitacionId = licitacion.Id,
            ProveedorId = proveedor.Id,
            MontoOfertadoCrc = 850_000m,
        });

        Assert.True(resultado.EsExito);
        Assert.Equal(850_000m, resultado.Valor!.MontoOfertadoCrc);
        Assert.Single(_contexto.Almacen.Ofertas);
    }

    [Fact]
    public async Task CrearAsync_ConSegundaOfertaDelMismoProveedor_DevuelveConflicto()
    {
        Licitacion licitacion = _contexto.SembrarLicitacion();
        Proveedor proveedor = _contexto.SembrarProveedor();
        _contexto.SembrarOferta(licitacion, proveedor, 800_000m);

        Resultado<OfertaDto> resultado = await _contexto.Ofertas.CrearAsync(new GuardarOfertaDto
        {
            LicitacionId = licitacion.Id,
            ProveedorId = proveedor.Id,
            MontoOfertadoCrc = 700_000m,
        });

        Assert.False(resultado.EsExito);
        Assert.Equal(CodigosError.OfertaDuplicada, resultado.Error!.Codigo);
        Assert.Single(_contexto.Almacen.Ofertas);
    }

    [Fact]
    public async Task CrearAsync_ConMontoSuperiorAlPresupuesto_Rechaza()
    {
        Licitacion licitacion = _contexto.SembrarLicitacion(presupuesto: 1_000_000m);
        Proveedor proveedor = _contexto.SembrarProveedor();

        Resultado<OfertaDto> resultado = await _contexto.Ofertas.CrearAsync(new GuardarOfertaDto
        {
            LicitacionId = licitacion.Id,
            ProveedorId = proveedor.Id,
            MontoOfertadoCrc = 1_500_000m,
        });

        Assert.False(resultado.EsExito);
        Assert.Equal(CodigosError.OfertaSuperaPresupuesto, resultado.Error!.Codigo);
        Assert.Empty(_contexto.Almacen.Ofertas);
    }

    [Fact]
    public async Task CrearAsync_TrasLaFechaDeCierre_RechazaPorVencimiento()
    {
        Licitacion licitacion = _contexto.SembrarLicitacion(diasParaCierre: 1);
        Proveedor proveedor = _contexto.SembrarProveedor();
        _contexto.Reloj.Avanzar(TimeSpan.FromDays(1));

        Resultado<OfertaDto> resultado = await _contexto.Ofertas.CrearAsync(new GuardarOfertaDto
        {
            LicitacionId = licitacion.Id,
            ProveedorId = proveedor.Id,
            MontoOfertadoCrc = 500_000m,
        });

        Assert.False(resultado.EsExito);
        Assert.Equal(CodigosError.OfertaVencida, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task CrearAsync_EnLicitacionNoPublicada_Rechaza()
    {
        Licitacion licitacion = _contexto.SembrarLicitacion(publicada: false);
        Proveedor proveedor = _contexto.SembrarProveedor();

        Resultado<OfertaDto> resultado = await _contexto.Ofertas.CrearAsync(new GuardarOfertaDto
        {
            LicitacionId = licitacion.Id,
            ProveedorId = proveedor.Id,
            MontoOfertadoCrc = 500_000m,
        });

        Assert.False(resultado.EsExito);
        Assert.Equal(CodigosError.OfertaLicitacionNoPublicada, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task CrearAsync_ConProveedorInexistente_DevuelveNoEncontrado()
    {
        Licitacion licitacion = _contexto.SembrarLicitacion();

        Resultado<OfertaDto> resultado = await _contexto.Ofertas.CrearAsync(new GuardarOfertaDto
        {
            LicitacionId = licitacion.Id,
            ProveedorId = Guid.NewGuid(),
            MontoOfertadoCrc = 500_000m,
        });

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task CrearAsync_ConProveedorEliminado_Rechaza()
    {
        Licitacion licitacion = _contexto.SembrarLicitacion();
        Proveedor proveedor = _contexto.SembrarProveedor();
        await _contexto.Proveedores.EliminarAsync(proveedor.Id);

        Resultado<OfertaDto> resultado = await _contexto.Ofertas.CrearAsync(new GuardarOfertaDto
        {
            LicitacionId = licitacion.Id,
            ProveedorId = proveedor.Id,
            MontoOfertadoCrc = 500_000m,
        });

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task ActualizarAsync_EnLicitacionVigente_CambiaElMonto()
    {
        Licitacion licitacion = _contexto.SembrarLicitacion();
        Oferta oferta = _contexto.SembrarOferta(licitacion, _contexto.SembrarProveedor(), 800_000m);

        Resultado<OfertaDto> resultado = await _contexto.Ofertas.ActualizarAsync(
            oferta.Id,
            new ActualizarOfertaDto { MontoOfertadoCrc = 750_000m });

        Assert.True(resultado.EsExito);
        Assert.Equal(750_000m, resultado.Valor!.MontoOfertadoCrc);
    }

    [Fact]
    public async Task ActualizarAsync_TrasElCierre_ConservaLaOfertaComoEvidencia()
    {
        Licitacion licitacion = _contexto.SembrarLicitacion();
        Oferta oferta = _contexto.SembrarOferta(licitacion, _contexto.SembrarProveedor(), 800_000m);
        licitacion.CambiarEstado(EstadoLicitacion.Cerrada, _contexto.Reloj.Ahora);

        Resultado<OfertaDto> resultado = await _contexto.Ofertas.ActualizarAsync(
            oferta.Id,
            new ActualizarOfertaDto { MontoOfertadoCrc = 100_000m });

        Assert.False(resultado.EsExito);
        Assert.Equal(800_000m, oferta.MontoOfertadoCrc);
    }

    [Fact]
    public async Task EliminarAsync_EnLicitacionVigente_EliminaLaOferta()
    {
        Licitacion licitacion = _contexto.SembrarLicitacion();
        Oferta oferta = _contexto.SembrarOferta(licitacion, _contexto.SembrarProveedor(), 800_000m);

        Resultado resultado = await _contexto.Ofertas.EliminarAsync(oferta.Id);

        Assert.True(resultado.EsExito);
        Assert.Empty(_contexto.Almacen.Ofertas);
    }

    [Fact]
    public async Task EliminarAsync_TrasElVencimiento_NoPermiteBorrarLaEvidencia()
    {
        Licitacion licitacion = _contexto.SembrarLicitacion(diasParaCierre: 1);
        Oferta oferta = _contexto.SembrarOferta(licitacion, _contexto.SembrarProveedor(), 800_000m);
        _contexto.Reloj.Avanzar(TimeSpan.FromDays(2));

        Resultado resultado = await _contexto.Ofertas.EliminarAsync(oferta.Id);

        Assert.False(resultado.EsExito);
        Assert.Equal(CodigosError.OfertaVencida, resultado.Error!.Codigo);
        Assert.Single(_contexto.Almacen.Ofertas);
    }

    [Fact]
    public async Task ListarAsync_FiltraPorLicitacionYPorProveedor()
    {
        Licitacion primera = _contexto.SembrarLicitacion("LIC-001");
        Licitacion segunda = _contexto.SembrarLicitacion("LIC-002");
        Proveedor proveedor = _contexto.SembrarProveedor("Alfa Servicios");
        Proveedor otro = _contexto.SembrarProveedor("Beta Servicios");

        _contexto.SembrarOferta(primera, proveedor, 100_000m);
        _contexto.SembrarOferta(segunda, proveedor, 200_000m);
        _contexto.SembrarOferta(primera, otro, 300_000m);

        PaginaResultado<OfertaDto> porLicitacion = await _contexto.Ofertas.ListarAsync(
            new ParametrosConsultaOfertas { LicitacionId = primera.Id });
        PaginaResultado<OfertaDto> porProveedor = await _contexto.Ofertas.ListarAsync(
            new ParametrosConsultaOfertas { ProveedorId = proveedor.Id });

        Assert.Equal(2, porLicitacion.TotalElementos);
        Assert.Equal(2, porProveedor.TotalElementos);
    }
}
