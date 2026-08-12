using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Comun;
using Licitaciones.UnitTests.Comun;

namespace Licitaciones.UnitTests.Aplicacion;

/// <summary>
/// Casos de uso de proveedores (historias H-01 a H-04).
/// </summary>
public sealed class ServicioProveedoresTests
{
    private readonly ContextoServicios _contexto = new();

    [Fact]
    public async Task CrearAsync_ConNombreValido_PersisteElProveedor()
    {
        Resultado<ProveedorDto> resultado = await _contexto.Proveedores.CrearAsync(
            new GuardarProveedorDto { Nombre = "  Constructora   del   Valle " });

        Assert.True(resultado.EsExito);
        Assert.Equal("Constructora del Valle", resultado.Valor!.Nombre);
        Assert.Single(_contexto.Almacen.Proveedores);
        Assert.Equal(1, _contexto.Almacen.VecesGuardado);
    }

    [Theory]
    [InlineData("Empresa Central")]
    [InlineData("empresa central")]
    [InlineData("EMPRESA   CENTRAL")]
    public async Task CrearAsync_ConNombreEquivalenteAUnoExistente_DevuelveConflicto(string nombreDuplicado)
    {
        await _contexto.Proveedores.CrearAsync(new GuardarProveedorDto { Nombre = "Empresa Central" });

        Resultado<ProveedorDto> resultado = await _contexto.Proveedores.CrearAsync(
            new GuardarProveedorDto { Nombre = nombreDuplicado });

        Assert.False(resultado.EsExito);
        Assert.Equal(CodigosError.NombreProveedorDuplicado, resultado.Error!.Codigo);
        Assert.Equal(TipoError.Conflicto, resultado.Error.Tipo);
        Assert.Single(_contexto.Almacen.Proveedores);
    }

    [Fact]
    public async Task CrearAsync_ConCaracteresNoPermitidos_DevuelveErrorDeReglaDeNegocio()
    {
        Resultado<ProveedorDto> resultado = await _contexto.Proveedores.CrearAsync(
            new GuardarProveedorDto { Nombre = "Empresa #1" });

        Assert.False(resultado.EsExito);
        Assert.Equal(CodigosError.NombreProveedorCaracteres, resultado.Error!.Codigo);
        Assert.Equal(TipoError.ReglaNegocio, resultado.Error.Tipo);
        Assert.Empty(_contexto.Almacen.Proveedores);
    }

    [Fact]
    public async Task ActualizarAsync_ConservandoSuPropioNombre_NoSeConsideraDuplicado()
    {
        Resultado<ProveedorDto> creado = await _contexto.Proveedores.CrearAsync(
            new GuardarProveedorDto { Nombre = "Empresa Central" });

        Resultado<ProveedorDto> resultado = await _contexto.Proveedores.ActualizarAsync(
            creado.Valor!.Id,
            new GuardarProveedorDto { Nombre = "EMPRESA CENTRAL" });

        Assert.True(resultado.EsExito);
    }

    [Fact]
    public async Task ActualizarAsync_ConNombreDeOtroProveedor_DevuelveConflicto()
    {
        await _contexto.Proveedores.CrearAsync(new GuardarProveedorDto { Nombre = "Empresa Central" });
        Resultado<ProveedorDto> segundo = await _contexto.Proveedores.CrearAsync(
            new GuardarProveedorDto { Nombre = "Constructora del Valle" });

        Resultado<ProveedorDto> resultado = await _contexto.Proveedores.ActualizarAsync(
            segundo.Valor!.Id,
            new GuardarProveedorDto { Nombre = "empresa central" });

        Assert.False(resultado.EsExito);
        Assert.Equal(CodigosError.NombreProveedorDuplicado, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task ActualizarAsync_ConIdentificadorInexistente_DevuelveNoEncontrado()
    {
        Resultado<ProveedorDto> resultado = await _contexto.Proveedores.ActualizarAsync(
            Guid.NewGuid(),
            new GuardarProveedorDto { Nombre = "Empresa Central" });

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task ActualizarAsync_ConVersionDelCliente_LaDeclaraParaElControlDeConcurrencia()
    {
        Resultado<ProveedorDto> creado = await _contexto.Proveedores.CrearAsync(
            new GuardarProveedorDto { Nombre = "Empresa Central" });

        await _contexto.Proveedores.ActualizarAsync(
            creado.Valor!.Id,
            new GuardarProveedorDto { Nombre = "Empresa Central del Sur", Version = 1 });

        Assert.Equal(1, _contexto.Almacen.VersionesOriginales[creado.Valor.Id]);
    }

    [Fact]
    public async Task EliminarAsync_AplicaBorradoLogicoYConservaLasOfertas()
    {
        var proveedor = _contexto.SembrarProveedor();
        var licitacion = _contexto.SembrarLicitacion();
        _contexto.SembrarOferta(licitacion, proveedor, 500_000m);

        Resultado resultado = await _contexto.Proveedores.EliminarAsync(proveedor.Id);

        Assert.True(resultado.EsExito);
        Assert.True(proveedor.EstaEliminado);
        Assert.Single(_contexto.Almacen.Ofertas);
    }

    [Fact]
    public async Task ListarAsync_OmiteLosProveedoresEliminadosSalvoQueSePidanExplicitamente()
    {
        var vigente = _contexto.SembrarProveedor("Alfa Servicios");
        var eliminado = _contexto.SembrarProveedor("Beta Servicios");
        await _contexto.Proveedores.EliminarAsync(eliminado.Id);

        PaginaResultado<ProveedorDto> soloVigentes = await _contexto.Proveedores.ListarAsync(
            new ParametrosConsultaProveedores());
        PaginaResultado<ProveedorDto> conEliminados = await _contexto.Proveedores.ListarAsync(
            new ParametrosConsultaProveedores { IncluirEliminados = true });

        Assert.Equal(vigente.Id, Assert.Single(soloVigentes.Elementos).Id);
        Assert.Equal(2, conEliminados.TotalElementos);
    }

    [Fact]
    public async Task ListarAsync_InformaLaCantidadDeOfertasDeCadaProveedor()
    {
        var proveedor = _contexto.SembrarProveedor();
        var licitacion = _contexto.SembrarLicitacion();
        _contexto.SembrarOferta(licitacion, proveedor, 400_000m);

        PaginaResultado<ProveedorDto> pagina = await _contexto.Proveedores.ListarAsync(
            new ParametrosConsultaProveedores());

        Assert.Equal(1, Assert.Single(pagina.Elementos).CantidadOfertas);
    }

    [Fact]
    public async Task ListarAsync_AplicaPaginacion()
    {
        for (int indice = 1; indice <= 5; indice++)
        {
            _contexto.SembrarProveedor($"Proveedor {indice}");
        }

        PaginaResultado<ProveedorDto> pagina = await _contexto.Proveedores.ListarAsync(
            new ParametrosConsultaProveedores { Pagina = 2, TamanoPagina = 2 });

        Assert.Equal(2, pagina.Elementos.Count);
        Assert.Equal(5, pagina.TotalElementos);
        Assert.Equal(3, pagina.TotalPaginas);
        Assert.True(pagina.TienePaginaAnterior);
        Assert.True(pagina.TienePaginaSiguiente);
    }

    [Fact]
    public async Task ObtenerAsync_ConProveedorEliminado_LoDevuelveMarcadoComoEliminado()
    {
        var proveedor = _contexto.SembrarProveedor();
        await _contexto.Proveedores.EliminarAsync(proveedor.Id);

        Resultado<ProveedorDto> resultado = await _contexto.Proveedores.ObtenerAsync(proveedor.Id);

        Assert.True(resultado.EsExito);
        Assert.True(resultado.Valor!.Eliminado);
    }
}
