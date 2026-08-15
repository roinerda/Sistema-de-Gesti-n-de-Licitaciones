using Licitaciones.Domain.Comun;
using Licitaciones.Infrastructure;
using Licitaciones.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Licitaciones.IntegrationTests.Comun;

/// <summary>
/// Levanta la aplicación completa (MVC + API) apuntando a la base de datos de la prueba.
/// </summary>
/// <remarks>
/// Se ejercita el mismo <c>Program</c> que se despliega en el contenedor: enrutado, versionado,
/// filtros, validación de modelo y traducción de errores a ProblemDetails. Solo se sustituyen el
/// contexto de datos, para aislar cada clase de prueba, y el reloj, para que las reglas
/// dependientes del tiempo sean deterministas.
/// </remarks>
public sealed class FabricaAplicacion : WebApplicationFactory<Program>
{
    private readonly string _cadenaConexion;
    private readonly IReloj _reloj;

    /// <summary>
    /// Crea la fábrica.
    /// </summary>
    /// <param name="cadenaConexion">Cadena de conexión de la base de datos de la prueba.</param>
    /// <param name="reloj">Reloj controlado que usará la aplicación.</param>
    public FabricaAplicacion(string cadenaConexion, IReloj reloj)
    {
        _cadenaConexion = cadenaConexion;
        _reloj = reloj;
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Entorno de producción: se prueba la tubería real, sin página de excepciones de desarrollo,
        // para confirmar que los errores nunca exponen trazas al cliente.
        builder.UseEnvironment(Environments.Production);

        builder.ConfigureTestServices(servicios =>
        {
            servicios.RemoveAll<DbContextOptions<LicitacionesDbContext>>();
            servicios.RemoveAll<DbContextOptions>();

            servicios.AddDbContext<LicitacionesDbContext>(opciones =>
                opciones.UseNpgsql(
                    _cadenaConexion,
                    npgsql => npgsql.MigrationsHistoryTable(
                        ConfiguracionInfraestructura.TablaHistorialMigraciones)));

            servicios.RemoveAll<IReloj>();
            servicios.AddSingleton(_reloj);
        });
    }
}
