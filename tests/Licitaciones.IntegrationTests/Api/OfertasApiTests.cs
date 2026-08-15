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
/// Ejercita <c>/api/v1/ofertas</c> sobre la aplicación completa y PostgreSQL real.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public sealed class OfertasApiTests : PruebaApi
{
    private const string Ruta = "/api/v1/ofertas";

    public OfertasApiTests(ContenedorPostgres contenedor)
        : base(contenedor)
    {
    }

    [Fact]
    public async Task Registrar_UnaOfertaIgualAlPresupuesto_EsAceptada()
    {
        Guid licitacionId = await CrearLicitacionPublicadaAsync("LIC-2026-010", 5_000_000m);
        Guid proveedorId = await CrearProveedorAsync("Constructora Alfa");

        using HttpResponseMessage respuesta = await RegistrarAsync(licitacionId, proveedorId, 5_000_000m);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        OfertaDto creada = await LeerAsync<OfertaDto>(respuesta);
        Assert.Equal(5_000_000m, creada.MontoOfertadoCrc);
        Assert.Equal("Constructora Alfa", creada.ProveedorNombre);
    }

    [Fact]
    public async Task Registrar_UnaOfertaMayorAlPresupuesto_EsRechazada()
    {
        Guid licitacionId = await CrearLicitacionPublicadaAsync("LIC-2026-011", 5_000_000m);
        Guid proveedorId = await CrearProveedorAsync("Constructora Alfa");

        using HttpResponseMessage respuesta = await RegistrarAsync(licitacionId, proveedorId, 5_000_000.01m);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);

        using JsonDocument problema = await LeerProblemaAsync(respuesta);
        Assert.Equal(
            CodigosError.OfertaSuperaPresupuesto,
            problema.RootElement.GetProperty("codigoError").GetString());
    }

    [Fact]
    public async Task Registrar_DosOfertasDelMismoProveedor_DevuelveConflicto()
    {
        Guid licitacionId = await CrearLicitacionPublicadaAsync("LIC-2026-012", 5_000_000m);
        Guid proveedorId = await CrearProveedorAsync("Constructora Alfa");

        using (HttpResponseMessage primera = await RegistrarAsync(licitacionId, proveedorId, 4_000_000m))
        {
            primera.EnsureSuccessStatusCode();
        }

        using HttpResponseMessage segunda = await RegistrarAsync(licitacionId, proveedorId, 3_000_000m);

        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);

        using JsonDocument problema = await LeerProblemaAsync(segunda);
        Assert.Equal(
            CodigosError.OfertaDuplicada,
            problema.RootElement.GetProperty("codigoError").GetString());
    }

    [Fact]
    public async Task Registrar_EnUnaLicitacionEnBorrador_EsRechazado()
    {
        Guid licitacionId = await CrearLicitacionAsync("LIC-2026-013", 5_000_000m);
        Guid proveedorId = await CrearProveedorAsync("Constructora Alfa");

        using HttpResponseMessage respuesta = await RegistrarAsync(licitacionId, proveedorId, 1_000_000m);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);

        using JsonDocument problema = await LeerProblemaAsync(respuesta);
        Assert.Equal(
            CodigosError.OfertaLicitacionNoPublicada,
            problema.RootElement.GetProperty("codigoError").GetString());
    }

    [Fact]
    public async Task Registrar_ConMontoCero_EsRechazadoAntesDeLlegarAlDominio()
    {
        Guid licitacionId = await CrearLicitacionPublicadaAsync("LIC-2026-014", 5_000_000m);
        Guid proveedorId = await CrearProveedorAsync("Constructora Alfa");

        using HttpResponseMessage respuesta = await RegistrarAsync(licitacionId, proveedorId, 0m);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Listar_FiltraPorLicitacionYOrdenaPorMonto()
    {
        Guid licitacionId = await CrearLicitacionPublicadaAsync("LIC-2026-015", 9_000_000m);
        Guid otraLicitacion = await CrearLicitacionPublicadaAsync("LIC-2026-016", 9_000_000m);

        Guid alfa = await CrearProveedorAsync("Constructora Alfa");
        Guid beta = await CrearProveedorAsync("Constructora Beta");

        (await RegistrarAsync(licitacionId, alfa, 8_000_000m)).Dispose();
        (await RegistrarAsync(licitacionId, beta, 6_500_000m)).Dispose();
        (await RegistrarAsync(otraLicitacion, alfa, 1_000_000m)).Dispose();

        using HttpResponseMessage respuesta = await Cliente.GetAsync(
            $"{Ruta}?LicitacionId={licitacionId}&OrdenarPor=monto");

        PaginaResultado<OfertaDto> pagina = await LeerAsync<PaginaResultado<OfertaDto>>(respuesta);

        Assert.Equal(2, pagina.TotalElementos);
        Assert.Equal(6_500_000m, pagina.Elementos[0].MontoOfertadoCrc);
        Assert.Equal(8_000_000m, pagina.Elementos[1].MontoOfertadoCrc);
    }

    [Fact]
    public async Task Eliminar_UnaOferta_DevuelveSinContenido()
    {
        Guid licitacionId = await CrearLicitacionPublicadaAsync("LIC-2026-017", 5_000_000m);
        Guid proveedorId = await CrearProveedorAsync("Constructora Alfa");

        OfertaDto creada;
        using (HttpResponseMessage registro = await RegistrarAsync(licitacionId, proveedorId, 4_000_000m))
        {
            registro.EnsureSuccessStatusCode();
            creada = await LeerAsync<OfertaDto>(registro);
        }

        using HttpResponseMessage eliminacion = await Cliente.DeleteAsync($"{Ruta}/{creada.Id}");
        Assert.Equal(HttpStatusCode.NoContent, eliminacion.StatusCode);

        using HttpResponseMessage consulta = await Cliente.GetAsync($"{Ruta}/{creada.Id}");
        Assert.Equal(HttpStatusCode.NotFound, consulta.StatusCode);
    }

    private Task<HttpResponseMessage> RegistrarAsync(Guid licitacionId, Guid proveedorId, decimal montoCrc) =>
        Cliente.PostAsJsonAsync(
            Ruta,
            new GuardarOfertaDto
            {
                LicitacionId = licitacionId,
                ProveedorId = proveedorId,
                MontoOfertadoCrc = montoCrc,
            },
            OpcionesJson);

    private async Task<Guid> CrearLicitacionAsync(string codigo, decimal presupuesto)
    {
        using HttpResponseMessage respuesta = await Cliente.PostAsJsonAsync(
            "/api/v1/licitaciones",
            new GuardarLicitacionDto
            {
                Codigo = codigo,
                Titulo = "Compra de equipo de cómputo",
                FechaCierre = Reloj.Ahora.AddDays(30),
                PresupuestoEstimadoCrc = presupuesto,
            },
            OpcionesJson);

        respuesta.EnsureSuccessStatusCode();
        LicitacionDto creada = await LeerAsync<LicitacionDto>(respuesta);
        return creada.Id;
    }

    private async Task<Guid> CrearLicitacionPublicadaAsync(string codigo, decimal presupuesto)
    {
        Guid id = await CrearLicitacionAsync(codigo, presupuesto);

        using HttpResponseMessage publicacion = await Cliente.PatchAsJsonAsync(
            $"/api/v1/licitaciones/{id}/estado",
            new CambiarEstadoLicitacionDto { NuevoEstado = EstadoLicitacion.Publicada },
            OpcionesJson);

        publicacion.EnsureSuccessStatusCode();
        return id;
    }

    private async Task<Guid> CrearProveedorAsync(string nombre)
    {
        using HttpResponseMessage respuesta = await Cliente.PostAsJsonAsync(
            "/api/v1/proveedores",
            new GuardarProveedorDto { Nombre = nombre },
            OpcionesJson);

        respuesta.EnsureSuccessStatusCode();
        ProveedorDto creado = await LeerAsync<ProveedorDto>(respuesta);
        return creado.Id;
    }
}
