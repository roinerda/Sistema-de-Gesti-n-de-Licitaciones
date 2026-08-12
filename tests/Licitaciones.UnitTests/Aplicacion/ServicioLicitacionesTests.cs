using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Enumeraciones;
using Licitaciones.UnitTests.Comun;

namespace Licitaciones.UnitTests.Aplicacion;

/// <summary>
/// Casos de uso de licitaciones (historias H-06, H-07, H-10 y H-11).
/// </summary>
public sealed class ServicioLicitacionesTests
{
    private readonly ContextoServicios _contexto = new();

    private GuardarLicitacionDto DatosValidos(string codigo = "LIC-001", decimal presupuesto = 1_000_000m) =>
        new()
        {
            Codigo = codigo,
            Titulo = "Compra de equipo de cómputo",
            FechaCierre = _contexto.Reloj.Ahora.AddDays(15),
            PresupuestoEstimadoCrc = presupuesto,
        };

    [Fact]
    public async Task CrearAsync_ConDatosValidos_CreaEnBorrador()
    {
        Resultado<LicitacionDto> resultado = await _contexto.Licitaciones.CrearAsync(DatosValidos());

        Assert.True(resultado.EsExito);
        Assert.Equal(EstadoLicitacion.Borrador, resultado.Valor!.Estado);
        Assert.Equal("Borrador", resultado.Valor.EstadoDescripcion);
    }

    [Theory]
    [InlineData("lic-001")]
    [InlineData("  LIC-001  ")]
    public async Task CrearAsync_ConCodigoEquivalenteAUnoExistente_DevuelveConflicto(string codigoDuplicado)
    {
        await _contexto.Licitaciones.CrearAsync(DatosValidos("LIC-001"));

        Resultado<LicitacionDto> resultado = await _contexto.Licitaciones.CrearAsync(DatosValidos(codigoDuplicado));

        Assert.False(resultado.EsExito);
        Assert.Equal(CodigosError.CodigoLicitacionDuplicado, resultado.Error!.Codigo);
        Assert.Single(_contexto.Almacen.Licitaciones);
    }

    [Fact]
    public async Task CrearAsync_ConPresupuestoCero_DevuelveErrorDeReglaDeNegocio()
    {
        Resultado<LicitacionDto> resultado = await _contexto.Licitaciones.CrearAsync(DatosValidos(presupuesto: 0m));

        Assert.False(resultado.EsExito);
        Assert.Equal(CodigosError.PresupuestoInvalido, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task CambiarEstadoAsync_DeBorradorAPublicada_Actualiza()
    {
        Resultado<LicitacionDto> creada = await _contexto.Licitaciones.CrearAsync(DatosValidos());

        Resultado<LicitacionDto> resultado = await _contexto.Licitaciones.CambiarEstadoAsync(
            creada.Valor!.Id,
            new CambiarEstadoLicitacionDto { NuevoEstado = EstadoLicitacion.Publicada });

        Assert.True(resultado.EsExito);
        Assert.Equal(EstadoLicitacion.Publicada, resultado.Valor!.Estado);
    }

    [Fact]
    public async Task CambiarEstadoAsync_DeCerradaAPublicada_DevuelveTransicionNoPermitida()
    {
        Licitacion licitacion = _contexto.SembrarLicitacion(publicada: false);
        await _contexto.Licitaciones.CambiarEstadoAsync(
            licitacion.Id,
            new CambiarEstadoLicitacionDto { NuevoEstado = EstadoLicitacion.Cerrada });

        Resultado<LicitacionDto> resultado = await _contexto.Licitaciones.CambiarEstadoAsync(
            licitacion.Id,
            new CambiarEstadoLicitacionDto { NuevoEstado = EstadoLicitacion.Publicada });

        Assert.False(resultado.EsExito);
        Assert.Equal(CodigosError.TransicionNoPermitida, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task ActualizarAsync_ConPresupuestoMenorAUnaOfertaRegistrada_DevuelveError()
    {
        Licitacion licitacion = _contexto.SembrarLicitacion(presupuesto: 1_000_000m);
        _contexto.SembrarOferta(licitacion, _contexto.SembrarProveedor(), 900_000m);

        GuardarLicitacionDto datos = DatosValidos(presupuesto: 800_000m);
        Resultado<LicitacionDto> resultado = await _contexto.Licitaciones.ActualizarAsync(licitacion.Id, datos);

        Assert.False(resultado.EsExito);
        Assert.Equal(CodigosError.PresupuestoMenorAOferta, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task ObtenerAsync_SinOfertas_DevuelveSinOfertasValidas()
    {
        Licitacion licitacion = _contexto.SembrarLicitacion();

        Resultado<LicitacionDetalleDto> resultado = await _contexto.Licitaciones.ObtenerAsync(licitacion.Id);

        Assert.True(resultado.EsExito);
        Assert.Null(resultado.Valor!.MejorOferta.Oferta);
        Assert.Equal(ClasificacionOferta.SinOfertasValidas, resultado.Valor.MejorOferta.Clasificacion);
        Assert.Equal("Sin ofertas válidas", resultado.Valor.MejorOferta.ClasificacionDescripcion);
        Assert.Null(resultado.Valor.MejorOferta.Aprobador);
    }

    [Fact]
    public async Task ObtenerMejorOfertaAsync_DevuelveMenorMontoClasificacionYAprobador()
    {
        _contexto.SembrarNivelesDelEnunciado();
        Licitacion licitacion = _contexto.SembrarLicitacion(presupuesto: 2_000_000m);
        _contexto.SembrarOferta(licitacion, _contexto.SembrarProveedor("Alfa Servicios"), 1_900_000m);
        _contexto.SembrarOferta(licitacion, _contexto.SembrarProveedor("Beta Servicios"), 1_500_000m);

        Resultado<MejorOfertaDto> resultado = await _contexto.Licitaciones.ObtenerMejorOfertaAsync(licitacion.Id);

        Assert.True(resultado.EsExito);
        Assert.Equal(1_500_000m, resultado.Valor!.Oferta!.MontoOfertadoCrc);
        Assert.Equal(25.00m, resultado.Valor.PorcentajeAhorro);
        Assert.Equal(ClasificacionOferta.OfertaConveniente, resultado.Valor.Clasificacion);
        Assert.Equal("Gerencia", resultado.Valor.Aprobador);
    }

    [Fact]
    public async Task ObtenerMejorOfertaAsync_EnEmpate_EligeLaRegistradaPrimero()
    {
        Licitacion licitacion = _contexto.SembrarLicitacion(presupuesto: 1_000_000m);
        Oferta primera = _contexto.SembrarOferta(licitacion, _contexto.SembrarProveedor("Alfa Servicios"), 800_000m);
        _contexto.Reloj.Avanzar(TimeSpan.FromMinutes(10));
        _contexto.SembrarOferta(licitacion, _contexto.SembrarProveedor("Beta Servicios"), 800_000m);

        Resultado<MejorOfertaDto> resultado = await _contexto.Licitaciones.ObtenerMejorOfertaAsync(licitacion.Id);

        Assert.Equal(primera.Id, resultado.Valor!.Oferta!.Id);
    }

    [Fact]
    public async Task ObtenerAsync_DevuelveLasTransicionesDisponiblesDesdeElEstadoActual()
    {
        Licitacion licitacion = _contexto.SembrarLicitacion(publicada: false);

        Resultado<LicitacionDetalleDto> resultado = await _contexto.Licitaciones.ObtenerAsync(licitacion.Id);

        Assert.Contains(EstadoLicitacion.Publicada, resultado.Valor!.TransicionesPermitidas);
        Assert.Contains(EstadoLicitacion.Cerrada, resultado.Valor.TransicionesPermitidas);
    }

    [Fact]
    public async Task ObtenerAsync_TrasElVencimiento_MarcaLaLicitacionComoCerradaFuncionalmente()
    {
        Licitacion licitacion = _contexto.SembrarLicitacion(diasParaCierre: 1);
        _contexto.Reloj.Avanzar(TimeSpan.FromDays(2));

        Resultado<LicitacionDetalleDto> resultado = await _contexto.Licitaciones.ObtenerAsync(licitacion.Id);

        Assert.Equal(EstadoLicitacion.Publicada, resultado.Valor!.Licitacion.Estado);
        Assert.True(resultado.Valor.Licitacion.CerradaFuncionalmente);
    }

    [Fact]
    public async Task EliminarAsync_AplicaBorradoLogicoYConservaLasOfertas()
    {
        Licitacion licitacion = _contexto.SembrarLicitacion();
        _contexto.SembrarOferta(licitacion, _contexto.SembrarProveedor(), 300_000m);

        Resultado resultado = await _contexto.Licitaciones.EliminarAsync(licitacion.Id);

        Assert.True(resultado.EsExito);
        Assert.True(licitacion.EstaEliminada);
        Assert.Single(_contexto.Almacen.Ofertas);
    }

    [Fact]
    public async Task ListarAsync_FiltraPorEstado()
    {
        _contexto.SembrarLicitacion("LIC-001", publicada: true);
        _contexto.SembrarLicitacion("LIC-002", publicada: false);

        PaginaResultado<LicitacionDto> pagina = await _contexto.Licitaciones.ListarAsync(
            new ParametrosConsultaLicitaciones { Estado = EstadoLicitacion.Borrador });

        Assert.Equal("LIC-002", Assert.Single(pagina.Elementos).Codigo);
    }

    [Fact]
    public async Task ObtenerAsync_ConIdentificadorInexistente_DevuelveNoEncontrado()
    {
        Resultado<LicitacionDetalleDto> resultado = await _contexto.Licitaciones.ObtenerAsync(Guid.NewGuid());

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
    }
}
