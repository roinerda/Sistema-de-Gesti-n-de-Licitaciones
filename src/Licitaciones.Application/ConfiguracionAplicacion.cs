using Licitaciones.Application.Servicios;
using Microsoft.Extensions.DependencyInjection;

namespace Licitaciones.Application;

/// <summary>
/// Registro de los casos de uso en el contenedor de inyección de dependencias.
/// </summary>
public static class ConfiguracionAplicacion
{
    /// <summary>
    /// Registra los servicios de aplicación con tiempo de vida por ámbito, igual que la unidad de trabajo.
    /// </summary>
    /// <param name="servicios">Colección de servicios de la aplicación.</param>
    /// <returns>La misma colección, para encadenar llamadas.</returns>
    public static IServiceCollection AgregarCapaAplicacion(this IServiceCollection servicios)
    {
        ArgumentNullException.ThrowIfNull(servicios);

        servicios.AddScoped<IServicioProveedores, ServicioProveedores>();
        servicios.AddScoped<IServicioLicitaciones, ServicioLicitaciones>();
        servicios.AddScoped<IServicioOfertas, ServicioOfertas>();
        servicios.AddScoped<IServicioNivelesAprobacion, ServicioNivelesAprobacion>();
        servicios.AddScoped<IServicioTiposCambio, ServicioTiposCambio>();
        servicios.AddScoped<IServicioConversionMoneda, ServicioConversionMoneda>();

        return servicios;
    }
}
