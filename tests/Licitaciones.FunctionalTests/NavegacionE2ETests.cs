using System.Globalization;
using Licitaciones.FunctionalTests.Comun;
using Microsoft.Playwright;

namespace Licitaciones.FunctionalTests;

/// <summary>
/// Recorridos de extremo a extremo con un navegador real sobre la aplicación desplegable.
/// </summary>
/// <remarks>
/// Cada prueba usa nombres y códigos irrepetibles porque comparten la misma base de datos: así se
/// mantiene el aislamiento sin reiniciar el contenedor entre pruebas.
/// </remarks>
[Collection(ColeccionE2E.Nombre)]
public sealed class NavegacionE2ETests
{
    private readonly ServidorE2E _servidor;

    public NavegacionE2ETests(ServidorE2E servidor) => _servidor = servidor;

    [Fact]
    public async Task PaginaDeInicio_PresentaLosModulosYElTipoDeCambioVigente()
    {
        (IBrowserContext contexto, IPage pagina) = await _servidor.AbrirAsync("/");
        await using var _ = contexto;

        await Assertions.Expect(pagina.GetByRole(AriaRole.Link, new() { Name = "Licitaciones", Exact = true }))
            .ToBeVisibleAsync();
        await Assertions.Expect(pagina.GetByRole(AriaRole.Link, new() { Name = "Proveedores", Exact = true }))
            .ToBeVisibleAsync();

        // El aviso permanente recuerda qué tipo de cambio se está aplicando y desde cuándo rige.
        await Assertions.Expect(pagina.Locator("#avisoTipoCambio")).ToContainTextAsync("CRC por USD");
    }

    [Fact]
    public async Task FlujoCompleto_LlevaDeLaLicitacionEnBorradorHastaLaMejorOferta()
    {
        string sufijo = Sufijo();
        string codigo = $"LIC-E2E-{sufijo}";
        string alfa = $"Constructora Alfa {sufijo}";
        string beta = $"Constructora Beta {sufijo}";

        (IBrowserContext contexto, IPage pagina) = await _servidor.AbrirAsync("/");
        await using var _ = contexto;

        await RegistrarProveedorAsync(pagina, alfa);
        await RegistrarProveedorAsync(pagina, beta);
        await CrearLicitacionAsync(pagina, codigo, 10_000_000m);

        // Se comprueba la insignia de estado, no el texto suelto: «Publicada» aparece también
        // dentro del botón «Pasar a Publicada» y una coincidencia parcial daría por buena una
        // transición que no ocurrió.
        await Assertions.Expect(pagina.Locator(".badge.estado-borrador")).ToBeVisibleAsync();

        // El cambio de estado pide confirmación. Playwright descarta los diálogos si nadie los
        // atiende, de modo que sin este manejador el formulario nunca llegaría a enviarse.
        pagina.Dialog += async (_, dialogo) => await dialogo.AcceptAsync();

        await pagina.GetByRole(AriaRole.Button, new() { Name = "Pasar a Publicada" }).ClickAsync();
        await Assertions.Expect(pagina.Locator(".badge.estado-publicada")).ToBeVisibleAsync();

        await RegistrarOfertaAsync(pagina, codigo, alfa, 9_500_000m);
        await RegistrarOfertaAsync(pagina, codigo, beta, 8_000_000m);

        // Coincidencia exacta en las tres: «Mejor oferta» aparece además dentro del encabezado
        // «Mejor oferta y aprobación», y una coincidencia parcial resolvería a dos elementos.
        await Assertions.Expect(pagina.GetByText("Oferta conveniente", new() { Exact = true }))
            .ToBeVisibleAsync();

        // La fila de la oferta ganadora queda marcada con su insignia.
        await Assertions.Expect(pagina.GetByText("Mejor oferta", new() { Exact = true }))
            .ToBeVisibleAsync();

        // El aprobador sale de la tabla parametrizable, no de una cadena de condicionales.
        await Assertions.Expect(pagina.GetByText("Gerencia", new() { Exact = true }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task Proveedor_ConNombreDuplicado_MuestraUnMensajeDeError()
    {
        string nombre = $"Suministros Gamma {Sufijo()}";

        (IBrowserContext contexto, IPage pagina) = await _servidor.AbrirAsync("/");
        await using var _ = contexto;

        await RegistrarProveedorAsync(pagina, nombre);

        // El mismo nombre con otras mayúsculas y espacios debe rechazarse.
        await pagina.GotoAsync("/Proveedores/Crear");
        await pagina.FillAsync("#Nombre", $"  {nombre.ToUpperInvariant()}  ");
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync();

        await Assertions.Expect(pagina.Locator(".alert-danger, .field-validation-error").First)
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task Licitacion_ConFechaDeCierrePasada_NoSeCrea()
    {
        (IBrowserContext contexto, IPage pagina) = await _servidor.AbrirAsync("/Licitaciones/Crear");
        await using var _ = contexto;

        await pagina.FillAsync("#Codigo", $"LIC-E2E-PAS-{Sufijo()}");
        await pagina.FillAsync("#Titulo", "Compra con fecha vencida");
        await pagina.FillAsync(
            "#FechaCierreLocal",
            DateTime.Now.AddDays(-2).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture));
        await pagina.FillAsync("#PresupuestoEstimadoCrc", "1000000");
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Crear en borrador" }).ClickAsync();

        await Assertions.Expect(pagina.Locator(".alert-danger, .field-validation-error").First)
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task AlternarMoneda_MuestraElEquivalenteEnDolaresSinCambiarElValorOficial()
    {
        (IBrowserContext contexto, IPage pagina) = await _servidor.AbrirAsync("/Licitaciones");
        await using var _ = contexto;

        await Assertions.Expect(pagina.Locator("body")).ToHaveAttributeAsync("data-moneda", "CRC");

        await pagina.Locator("#botonMoneda").ClickAsync();

        await Assertions.Expect(pagina.Locator("body")).ToHaveAttributeAsync("data-moneda", "USD");
        await Assertions.Expect(pagina.Locator("#etiquetaMoneda")).ToHaveTextAsync("CRC");
    }

    [Fact]
    public async Task AlternarTema_CambiaElTemaYLoConservaAlNavegar()
    {
        (IBrowserContext contexto, IPage pagina) = await _servidor.AbrirAsync("/");
        await using var _ = contexto;

        await Assertions.Expect(pagina.Locator("html")).ToHaveAttributeAsync("data-bs-theme", "light");

        await pagina.Locator("#botonTema").ClickAsync();
        await Assertions.Expect(pagina.Locator("html")).ToHaveAttributeAsync("data-bs-theme", "dark");

        // La preferencia se aplica antes del primer pintado de la página siguiente.
        await pagina.GotoAsync("/Proveedores");
        await Assertions.Expect(pagina.Locator("html")).ToHaveAttributeAsync("data-bs-theme", "dark");
    }

    [Fact]
    public async Task Eliminar_UnProveedor_PideConfirmacionAntesDeBorrar()
    {
        string nombre = $"Servicios Delta {Sufijo()}";

        (IBrowserContext contexto, IPage pagina) = await _servidor.AbrirAsync("/");
        await using var _ = contexto;

        await RegistrarProveedorAsync(pagina, nombre);

        // Se filtra por el nombre en lugar de confiar en la primera página del listado: toda la
        // serie comparte una base de datos y los proveedores de las demás pruebas se acumulan.
        await pagina.GotoAsync($"/Proveedores?Buscar={Uri.EscapeDataString(nombre)}");
        await pagina.GetByRole(AriaRole.Row, new() { Name = nombre })
            .GetByRole(AriaRole.Link, new() { Name = "Eliminar" })
            .ClickAsync();

        // La primera vez se cancela: el proveedor debe seguir apareciendo.
        pagina.Dialog += async (_, dialogo) => await dialogo.DismissAsync();
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Sí, eliminar" }).ClickAsync();

        await Assertions.Expect(pagina.GetByRole(AriaRole.Button, new() { Name = "Sí, eliminar" }))
            .ToBeVisibleAsync();
    }

    private static string Sufijo() => Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    private static async Task RegistrarProveedorAsync(IPage pagina, string nombre)
    {
        await pagina.GotoAsync("/Proveedores/Crear");
        await pagina.FillAsync("#Nombre", nombre);
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync();
        await Assertions.Expect(pagina.Locator(".alert-success")).ToBeVisibleAsync();
    }

    private static async Task CrearLicitacionAsync(IPage pagina, string codigo, decimal presupuestoCrc)
    {
        await pagina.GotoAsync("/Licitaciones/Crear");
        await pagina.FillAsync("#Codigo", codigo);
        await pagina.FillAsync("#Titulo", "Compra de equipo de cómputo");
        await pagina.FillAsync(
            "#FechaCierreLocal",
            DateTime.Now.AddDays(30).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture));
        await pagina.FillAsync(
            "#PresupuestoEstimadoCrc",
            presupuestoCrc.ToString("0.##", CultureInfo.InvariantCulture));

        await pagina.GetByRole(AriaRole.Button, new() { Name = "Crear en borrador" }).ClickAsync();
        await Assertions.Expect(pagina.GetByRole(AriaRole.Heading, new() { Name = codigo })).ToBeVisibleAsync();
    }

    private static async Task RegistrarOfertaAsync(
        IPage pagina,
        string codigoLicitacion,
        string proveedor,
        decimal montoCrc)
    {
        await pagina.GotoAsync("/Ofertas/Crear");

        // Se selecciona por texto visible para no depender de los identificadores generados.
        await pagina.Locator("#LicitacionId").SelectOptionAsync(
            await ValorDeOpcionAsync(pagina, "#LicitacionId", codigoLicitacion));
        await pagina.Locator("#ProveedorId").SelectOptionAsync(
            await ValorDeOpcionAsync(pagina, "#ProveedorId", proveedor));

        await pagina.FillAsync("#MontoOfertadoCrc", montoCrc.ToString("0.##", CultureInfo.InvariantCulture));
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Registrar oferta" }).ClickAsync();
        await Assertions.Expect(pagina.Locator(".alert-success")).ToBeVisibleAsync();
    }

    private static async Task<string> ValorDeOpcionAsync(IPage pagina, string selector, string textoBuscado)
    {
        IReadOnlyList<IElementHandle> opciones = await pagina.Locator($"{selector} option").ElementHandlesAsync();

        foreach (IElementHandle opcion in opciones)
        {
            string texto = await opcion.TextContentAsync() ?? string.Empty;

            if (texto.Contains(textoBuscado, StringComparison.Ordinal))
            {
                return await opcion.GetAttributeAsync("value") ?? string.Empty;
            }
        }

        throw new InvalidOperationException($"No se encontró la opción «{textoBuscado}» en {selector}.");
    }
}
