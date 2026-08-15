using System.Net;
using System.Text.Json;
using Licitaciones.IntegrationTests.Comun;

namespace Licitaciones.IntegrationTests.Api;

/// <summary>
/// Comprueba que la aplicación desplegable exponga la interfaz web, la API, la documentación
/// OpenAPI y las sondas de salud que consumen Docker y Kubernetes.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public sealed class ArranqueAplicacionTests : PruebaApi
{
    public ArranqueAplicacionTests(ContenedorPostgres contenedor)
        : base(contenedor)
    {
    }

    [Fact]
    public async Task PaginaDeInicio_RespondeConHtml()
    {
        using HttpResponseMessage respuesta = await Cliente.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("text/html", respuesta.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("/salud/vivo")]
    [InlineData("/salud/listo")]
    public async Task SondasDeSalud_RespondenCorrectamente(string ruta)
    {
        using HttpResponseMessage respuesta = await Cliente.GetAsync(ruta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Healthy", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DocumentoOpenApi_DescribeLaVersionUnoDeLaApi()
    {
        using HttpResponseMessage respuesta = await Cliente.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        using JsonDocument documento = await LeerProblemaAsync(respuesta);
        JsonElement rutas = documento.RootElement.GetProperty("paths");

        Assert.True(rutas.TryGetProperty("/api/v1/proveedores", out _));
        Assert.True(rutas.TryGetProperty("/api/v1/licitaciones", out _));
        Assert.True(rutas.TryGetProperty("/api/v1/ofertas", out _));
        Assert.True(rutas.TryGetProperty("/api/v1/niveles-aprobacion", out _));
        Assert.True(rutas.TryGetProperty("/api/v1/tipos-cambio", out _));
    }

    [Fact]
    public async Task VersionInexistenteDeLaApi_NoResuelveNingunEndpoint()
    {
        using HttpResponseMessage respuesta = await Cliente.GetAsync("/api/v9/proveedores");

        Assert.NotEqual(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task TipoCambioActivo_SeExponeParaLaConversionReferencial()
    {
        using HttpResponseMessage respuesta = await Cliente.GetAsync(
            "/api/v1/tipos-cambio/conversion?montoCrc=1040000");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        using JsonDocument documento = await LeerProblemaAsync(respuesta);
        Assert.Equal(2000m, documento.RootElement.GetProperty("montoUsd").GetDecimal());
        Assert.Equal(520m, documento.RootElement.GetProperty("crcPorUsd").GetDecimal());
    }
}
