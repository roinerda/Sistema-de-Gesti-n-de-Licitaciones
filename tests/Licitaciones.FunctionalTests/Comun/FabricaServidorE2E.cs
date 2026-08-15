using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Licitaciones.FunctionalTests.Comun;

/// <summary>
/// Levanta la aplicación sobre Kestrel en un puerto libre para que un navegador real la visite.
/// </summary>
/// <remarks>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> usa por omisión un servidor en memoria al que
/// Playwright no puede conectarse. Aquí se construyen dos hosts a partir del mismo constructor: uno
/// sobre Kestrel, que atiende las peticiones del navegador, y el que exige la infraestructura de
/// pruebas. La aplicación que se ejercita es exactamente la que se publica en el contenedor.
/// </remarks>
public sealed class FabricaServidorE2E : WebApplicationFactory<Program>
{
    private IHost? _hostKestrel;

    /// <summary>Dirección HTTP donde quedó escuchando la aplicación.</summary>
    public string DireccionBase { get; private set; } = string.Empty;

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Entorno de producción: se recorre la interfaz tal como la vería una persona usuaria real.
        builder.UseEnvironment(Environments.Production);
    }

    /// <inheritdoc />
    protected override IHost CreateHost(IHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // El host en memoria debe construirse antes de cambiar el servidor a Kestrel.
        IHost hostEnMemoria = builder.Build();

        builder.ConfigureWebHost(web => web
            .UseKestrel()
            .UseUrls("http://127.0.0.1:0"));

        _hostKestrel = builder.Build();
        _hostKestrel.Start();

        IServerAddressesFeature? direcciones = _hostKestrel.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>();

        DireccionBase = direcciones?.Addresses.FirstOrDefault()
            ?? throw new InvalidOperationException("Kestrel no publicó ninguna dirección.");

        ClientOptions.BaseAddress = new Uri(DireccionBase);

        hostEnMemoria.Start();
        return hostEnMemoria;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hostKestrel?.Dispose();
            _hostKestrel = null;
        }

        base.Dispose(disposing);
    }
}
