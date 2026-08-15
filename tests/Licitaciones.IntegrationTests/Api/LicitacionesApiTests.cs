using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Enumeraciones;
using Licitaciones.IntegrationTests.Comun;

namespace Licitaciones.IntegrationTests.Api;

/// <summary>
/// Ejercita <c>/api/v1/licitaciones</c> sobre la aplicación completa y PostgreSQL real.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public sealed class LicitacionesApiTests : PruebaApi
{
    private const string Ruta = "/api/v1/licitaciones";

    public LicitacionesApiTests(ContenedorPostgres contenedor)
        : base(contenedor)
    {
    }

    [Fact]
    public async Task Crear_DejaLaLicitacionEnBorrador()
    {
        LicitacionDto creada = await CrearLicitacionAsync("LIC-2026-001");

        Assert.Equal(EstadoLicitacion.Borrador, creada.Estado);
        Assert.Equal("LIC-2026-001", creada.Codigo);
        Assert.False(creada.CerradaFuncionalmente);
    }

    [Fact]
    public async Task Crear_ConFechaDeCierrePasada_DevuelveErrorDeValidacion()
    {
        using HttpResponseMessage respuesta = await Cliente.PostAsJsonAsync(
            Ruta,
            new GuardarLicitacionDto
            {
                Codigo = "LIC-2026-090",
                Titulo = "Compra vencida",
                FechaCierre = Reloj.Ahora.AddDays(-1),
                PresupuestoEstimadoCrc = 1_000_000m,
            },
            OpcionesJson);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);

        using JsonDocument problema = await LeerProblemaAsync(respuesta);
        Assert.Equal(
            CodigosError.FechaCierreInvalida,
            problema.RootElement.GetProperty("codigoError").GetString());
    }

    [Fact]
    public async Task Crear_ConCodigoDuplicadoNormalizado_DevuelveConflicto()
    {
        await CrearLicitacionAsync("LIC-2026-001");

        using HttpResponseMessage respuesta = await Cliente.PostAsJsonAsync(
            Ruta,
            new GuardarLicitacionDto
            {
                Codigo = "  lic-2026-001 ",
                Titulo = "Otra compra",
                FechaCierre = Reloj.Ahora.AddDays(20),
                PresupuestoEstimadoCrc = 2_000_000m,
            },
            OpcionesJson);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);

        using JsonDocument problema = await LeerProblemaAsync(respuesta);
        Assert.Equal(
            CodigosError.CodigoLicitacionDuplicado,
            problema.RootElement.GetProperty("codigoError").GetString());
    }

    [Theory]
    [InlineData(EstadoLicitacion.Publicada)]
    [InlineData(EstadoLicitacion.Cerrada)]
    public async Task CambiarEstado_DesdeBorrador_PermiteLasDosTransicionesDeclaradas(EstadoLicitacion destino)
    {
        LicitacionDto creada = await CrearLicitacionAsync($"LIC-2026-{(int)destino:000}");

        using HttpResponseMessage respuesta = await CambiarEstadoAsync(creada.Id, destino);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        LicitacionDto actualizada = await LeerAsync<LicitacionDto>(respuesta);
        Assert.Equal(destino, actualizada.Estado);
    }

    [Fact]
    public async Task CambiarEstado_DePublicadaABorrador_EsRechazado()
    {
        LicitacionDto creada = await CrearLicitacionAsync("LIC-2026-002");
        using (HttpResponseMessage publicacion = await CambiarEstadoAsync(creada.Id, EstadoLicitacion.Publicada))
        {
            publicacion.EnsureSuccessStatusCode();
        }

        using HttpResponseMessage respuesta = await CambiarEstadoAsync(creada.Id, EstadoLicitacion.Borrador);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);

        using JsonDocument problema = await LeerProblemaAsync(respuesta);
        Assert.Equal(
            CodigosError.TransicionNoPermitida,
            problema.RootElement.GetProperty("codigoError").GetString());
    }

    [Fact]
    public async Task CambiarEstado_DeCerradaAPublicada_EsRechazado()
    {
        LicitacionDto creada = await CrearLicitacionAsync("LIC-2026-003");
        using (HttpResponseMessage cierre = await CambiarEstadoAsync(creada.Id, EstadoLicitacion.Cerrada))
        {
            cierre.EnsureSuccessStatusCode();
        }

        using HttpResponseMessage respuesta = await CambiarEstadoAsync(creada.Id, EstadoLicitacion.Publicada);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
    }

    [Fact]
    public async Task MejorOferta_SinOfertas_DevuelveElTextoDelEnunciado()
    {
        LicitacionDto creada = await CrearLicitacionAsync("LIC-2026-004");

        using HttpResponseMessage respuesta = await Cliente.GetAsync($"{Ruta}/{creada.Id}/mejor-oferta");
        MejorOfertaDto mejor = await LeerAsync<MejorOfertaDto>(respuesta);

        Assert.Null(mejor.Oferta);
        Assert.Equal("Sin ofertas válidas", mejor.ClasificacionDescripcion);
        Assert.Null(mejor.Aprobador);
    }

    [Fact]
    public async Task MejorOferta_ConAhorroDeAlMenosDiezPorCiento_EsConvenienteYUsaLaTablaDeAprobadores()
    {
        LicitacionDto creada = await CrearLicitacionAsync("LIC-2026-005", presupuesto: 10_000_000m);
        await CambiarEstadoAsync(creada.Id, EstadoLicitacion.Publicada);

        ProveedorDto alfa = await CrearProveedorAsync("Constructora Alfa");
        ProveedorDto beta = await CrearProveedorAsync("Constructora Beta");

        await RegistrarOfertaAsync(creada.Id, alfa.Id, 9_500_000m);
        await RegistrarOfertaAsync(creada.Id, beta.Id, 8_000_000m);

        using HttpResponseMessage respuesta = await Cliente.GetAsync($"{Ruta}/{creada.Id}/mejor-oferta");
        MejorOfertaDto mejor = await LeerAsync<MejorOfertaDto>(respuesta);

        Assert.NotNull(mejor.Oferta);
        Assert.Equal(8_000_000m, mejor.Oferta!.MontoOfertadoCrc);
        Assert.Equal(20m, mejor.PorcentajeAhorro);
        Assert.Equal("Oferta conveniente", mejor.ClasificacionDescripcion);

        // El aprobador proviene de la tabla parametrizable sembrada por la migración.
        Assert.Equal("Gerencia", mejor.Aprobador);
    }

    [Fact]
    public async Task Eliminar_ConOfertasRegistradas_AplicaBorradoLogicoYConservaLasOfertas()
    {
        LicitacionDto creada = await CrearLicitacionAsync("LIC-2026-006");
        await CambiarEstadoAsync(creada.Id, EstadoLicitacion.Publicada);

        ProveedorDto proveedor = await CrearProveedorAsync("Constructora Alfa");
        await RegistrarOfertaAsync(creada.Id, proveedor.Id, 1_000_000m);

        // El borrado es lógico y no se bloquea por tener ofertas: la regla del enunciado es
        // conservarlas como evidencia, no impedir la baja del expediente.
        using HttpResponseMessage respuesta = await Cliente.DeleteAsync($"{Ruta}/{creada.Id}");
        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);

        // La licitación desaparece del listado ordinario.
        using HttpResponseMessage listado = await Cliente.GetAsync(Ruta);
        PaginaResultado<LicitacionDto> pagina = await LeerAsync<PaginaResultado<LicitacionDto>>(listado);
        Assert.DoesNotContain(pagina.Elementos, l => l.Id == creada.Id);

        // Pero la oferta sigue registrada: es la evidencia de por qué se adjudicó lo que se adjudicó.
        using HttpResponseMessage ofertas = await Cliente.GetAsync($"{Ruta}/{creada.Id}/ofertas");
        PaginaResultado<OfertaDto> registradas = await LeerAsync<PaginaResultado<OfertaDto>>(ofertas);

        Assert.Single(registradas.Elementos);
        Assert.Equal(1_000_000m, registradas.Elementos[0].MontoOfertadoCrc);
        Assert.Equal("Constructora Alfa", registradas.Elementos[0].ProveedorNombre);
    }

    [Fact]
    public async Task Detalle_IncluyeLasTransicionesPermitidasDesdeElEstadoActual()
    {
        LicitacionDto creada = await CrearLicitacionAsync("LIC-2026-007");

        using HttpResponseMessage respuesta = await Cliente.GetAsync($"{Ruta}/{creada.Id}");
        LicitacionDetalleDto detalle = await LeerAsync<LicitacionDetalleDto>(respuesta);

        Assert.Equal(
            [EstadoLicitacion.Publicada, EstadoLicitacion.Cerrada],
            detalle.TransicionesPermitidas.OrderBy(e => e).ToArray());
    }

    private async Task<LicitacionDto> CrearLicitacionAsync(string codigo, decimal presupuesto = 5_000_000m)
    {
        using HttpResponseMessage respuesta = await Cliente.PostAsJsonAsync(
            Ruta,
            new GuardarLicitacionDto
            {
                Codigo = codigo,
                Titulo = "Compra de equipo de cómputo",
                FechaCierre = Reloj.Ahora.AddDays(30),
                PresupuestoEstimadoCrc = presupuesto,
            },
            OpcionesJson);

        respuesta.EnsureSuccessStatusCode();
        return await LeerAsync<LicitacionDto>(respuesta);
    }

    private async Task<ProveedorDto> CrearProveedorAsync(string nombre)
    {
        using HttpResponseMessage respuesta = await Cliente.PostAsJsonAsync(
            "/api/v1/proveedores",
            new GuardarProveedorDto { Nombre = nombre },
            OpcionesJson);

        respuesta.EnsureSuccessStatusCode();
        return await LeerAsync<ProveedorDto>(respuesta);
    }

    private async Task RegistrarOfertaAsync(Guid licitacionId, Guid proveedorId, decimal montoCrc)
    {
        using HttpResponseMessage respuesta = await Cliente.PostAsJsonAsync(
            $"{Ruta}/{licitacionId}/ofertas",
            new CrearOfertaDto { ProveedorId = proveedorId, MontoOfertadoCrc = montoCrc },
            OpcionesJson);

        respuesta.EnsureSuccessStatusCode();
    }

    private Task<HttpResponseMessage> CambiarEstadoAsync(Guid id, EstadoLicitacion destino) =>
        Cliente.PatchAsJsonAsync(
            $"{Ruta}/{id}/estado",
            new CambiarEstadoLicitacionDto { NuevoEstado = destino },
            OpcionesJson);
}
