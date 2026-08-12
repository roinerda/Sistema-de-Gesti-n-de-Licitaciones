using Licitaciones.Application.Abstracciones;
using Licitaciones.Domain.Comun;
using Licitaciones.Infrastructure.Persistencia;
using Licitaciones.Infrastructure.Persistencia.Repositorios;
using Licitaciones.Infrastructure.Reloj;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Licitaciones.Infrastructure;

/// <summary>
/// Registro de la infraestructura en el contenedor de inyección de dependencias.
/// </summary>
public static class ConfiguracionInfraestructura
{
    /// <summary>Nombre de la cadena de conexión esperada en la configuración.</summary>
    public const string NombreCadenaConexion = "Licitaciones";

    /// <summary>Nombre de la tabla de historial de migraciones.</summary>
    public const string TablaHistorialMigraciones = "__ef_migraciones_historial";

    /// <summary>
    /// Registra el contexto, los repositorios, la unidad de trabajo y el reloj real.
    /// </summary>
    /// <param name="servicios">Colección de servicios de la aplicación.</param>
    /// <param name="configuracion">Configuración de la aplicación.</param>
    /// <returns>La misma colección, para encadenar llamadas.</returns>
    /// <exception cref="InvalidOperationException">Si no hay una cadena de conexión configurada.</exception>
    public static IServiceCollection AgregarCapaInfraestructura(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentNullException.ThrowIfNull(configuracion);

        string cadenaConexion = ResolverCadenaConexion(configuracion);

        servicios.AddDbContext<LicitacionesDbContext>(opciones =>
            opciones.UseNpgsql(cadenaConexion, npgsql =>
            {
                npgsql.MigrationsHistoryTable(TablaHistorialMigraciones);

                // Reintentos ante errores transitorios: útil cuando el contenedor de PostgreSQL
                // todavía está iniciando o durante un reinicio del pod en Kubernetes.
                npgsql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
            }));

        servicios.AddSingleton<IReloj, RelojSistema>();
        servicios.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();
        servicios.AddScoped<IRepositorioProveedores, RepositorioProveedores>();
        servicios.AddScoped<IRepositorioLicitaciones, RepositorioLicitaciones>();
        servicios.AddScoped<IRepositorioOfertas, RepositorioOfertas>();
        servicios.AddScoped<IRepositorioNivelesAprobacion, RepositorioNivelesAprobacion>();
        servicios.AddScoped<IRepositorioTiposCambio, RepositorioTiposCambio>();
        servicios.AddScoped<InicializadorBaseDatos>();

        servicios.AddHealthChecks()
            .AddDbContextCheck<LicitacionesDbContext>(
                name: "base-datos",
                tags: ["listo"]);

        return servicios;
    }

    /// <summary>
    /// Obtiene la cadena de conexión desde la configuración o desde variables de entorno.
    /// </summary>
    /// <param name="configuracion">Configuración de la aplicación.</param>
    /// <returns>Cadena de conexión válida.</returns>
    /// <exception cref="InvalidOperationException">Si no hay ninguna cadena configurada.</exception>
    /// <remarks>
    /// Nunca se incrusta una cadena real en el repositorio: se toma de <c>ConnectionStrings:Licitaciones</c>,
    /// que en contenedores y en Kubernetes se define mediante la variable de entorno
    /// <c>ConnectionStrings__Licitaciones</c> respaldada por un Secret.
    /// </remarks>
    public static string ResolverCadenaConexion(IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(configuracion);

        string? cadena = configuracion.GetConnectionString(NombreCadenaConexion);

        if (string.IsNullOrWhiteSpace(cadena))
        {
            throw new InvalidOperationException(
                "No hay cadena de conexión configurada. Defina ConnectionStrings:Licitaciones " +
                "o la variable de entorno ConnectionStrings__Licitaciones.");
        }

        return cadena;
    }
}
