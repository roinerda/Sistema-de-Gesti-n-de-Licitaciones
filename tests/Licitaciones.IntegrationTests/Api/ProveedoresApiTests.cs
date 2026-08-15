using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Comun;
using Licitaciones.IntegrationTests.Comun;

namespace Licitaciones.IntegrationTests.Api;

/// <summary>
/// Ejercita <c>/api/v1/proveedores</c> sobre la aplicación completa y PostgreSQL real.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public sealed class ProveedoresApiTests : PruebaApi
{
    private const string Ruta = "/api/v1/proveedores";

    public ProveedoresApiTests(ContenedorPostgres contenedor)
        : base(contenedor)
    {
    }

    [Fact]
    public async Task Crear_DevuelveCreadoConLaCabeceraLocation()
    {
        using HttpResponseMessage respuesta = await Cliente.PostAsJsonAsync(
            Ruta,
            new GuardarProveedorDto { Nombre = "Constructora Alfa" },
            OpcionesJson);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.NotNull(respuesta.Headers.Location);

        ProveedorDto creado = await LeerAsync<ProveedorDto>(respuesta);
        Assert.Equal("Constructora Alfa", creado.Nombre);
        Assert.Equal("CONSTRUCTORA ALFA", creado.NombreNormalizado);

        using HttpResponseMessage consulta = await Cliente.GetAsync(respuesta.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
    }

    [Fact]
    public async Task Crear_ConNombreDuplicadoNormalizado_DevuelveConflicto()
    {
        await CrearProveedorAsync("Constructora Alfa");

        using HttpResponseMessage respuesta = await Cliente.PostAsJsonAsync(
            Ruta,
            new GuardarProveedorDto { Nombre = "  constructora   ALFA " },
            OpcionesJson);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);

        using JsonDocument problema = await LeerProblemaAsync(respuesta);
        Assert.Equal(409, problema.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(
            problema.RootElement.GetProperty("codigoError").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(
            problema.RootElement.GetProperty("identificadorCorrelacion").GetString()));
    }

    [Fact]
    public async Task Crear_ConCaracteresNoPermitidos_DevuelveErrorDeValidacion()
    {
        using HttpResponseMessage respuesta = await Cliente.PostAsJsonAsync(
            Ruta,
            new GuardarProveedorDto { Nombre = "Constructora <script>" },
            OpcionesJson);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        using JsonDocument problema = await LeerProblemaAsync(respuesta);
        Assert.Equal(
            CodigosError.ValidacionEntrada,
            problema.RootElement.GetProperty("codigoError").GetString());
        Assert.True(problema.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Obtener_ConIdentificadorInexistente_DevuelveNoEncontrado()
    {
        using HttpResponseMessage respuesta = await Cliente.GetAsync($"{Ruta}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);

        using JsonDocument problema = await LeerProblemaAsync(respuesta);
        Assert.Equal("Recurso no encontrado", problema.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Listar_AplicaPaginacionYBusqueda()
    {
        await CrearProveedorAsync("Constructora Alfa");
        await CrearProveedorAsync("Constructora Beta");
        await CrearProveedorAsync("Suministros Gamma");

        using HttpResponseMessage respuesta = await Cliente.GetAsync(
            $"{Ruta}?Buscar=constructora&Pagina=1&TamanoPagina=1");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        PaginaResultado<ProveedorDto> pagina = await LeerAsync<PaginaResultado<ProveedorDto>>(respuesta);

        Assert.Single(pagina.Elementos);
        Assert.Equal(2, pagina.TotalElementos);
        Assert.Equal(2, pagina.TotalPaginas);
    }

    [Fact]
    public async Task Actualizar_ConVersionDesactualizada_DevuelveConflictoDeConcurrencia()
    {
        ProveedorDto proveedor = await CrearProveedorAsync("Constructora Alfa");

        using HttpResponseMessage primera = await Cliente.PutAsJsonAsync(
            $"{Ruta}/{proveedor.Id}",
            new GuardarProveedorDto { Nombre = "Constructora Alfa S.A.", Version = proveedor.Version },
            OpcionesJson);

        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);

        using HttpResponseMessage segunda = await Cliente.PutAsJsonAsync(
            $"{Ruta}/{proveedor.Id}",
            new GuardarProveedorDto { Nombre = "Constructora Alfa Limitada", Version = proveedor.Version },
            OpcionesJson);

        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);

        using JsonDocument problema = await LeerProblemaAsync(segunda);
        Assert.Equal(
            CodigosError.Concurrencia,
            problema.RootElement.GetProperty("codigoError").GetString());
    }

    [Fact]
    public async Task Eliminar_DevuelveSinContenidoYOcultaElProveedorDelListado()
    {
        ProveedorDto proveedor = await CrearProveedorAsync("Constructora Alfa");

        using HttpResponseMessage eliminacion = await Cliente.DeleteAsync($"{Ruta}/{proveedor.Id}");
        Assert.Equal(HttpStatusCode.NoContent, eliminacion.StatusCode);

        using HttpResponseMessage listado = await Cliente.GetAsync(Ruta);
        PaginaResultado<ProveedorDto> pagina = await LeerAsync<PaginaResultado<ProveedorDto>>(listado);

        Assert.Empty(pagina.Elementos);
    }

    [Fact]
    public async Task RespuestasDeError_NuncaExponenTrazasNiRutasInternas()
    {
        using HttpResponseMessage respuesta = await Cliente.GetAsync($"{Ruta}/{Guid.NewGuid()}");
        string cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.DoesNotContain("Licitaciones.Infrastructure", cuerpo, StringComparison.Ordinal);
        Assert.DoesNotContain("Npgsql", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("C:\\", cuerpo, StringComparison.Ordinal);
    }

    private async Task<ProveedorDto> CrearProveedorAsync(string nombre)
    {
        using HttpResponseMessage respuesta = await Cliente.PostAsJsonAsync(
            Ruta,
            new GuardarProveedorDto { Nombre = nombre },
            OpcionesJson);

        respuesta.EnsureSuccessStatusCode();
        return await LeerAsync<ProveedorDto>(respuesta);
    }
}
