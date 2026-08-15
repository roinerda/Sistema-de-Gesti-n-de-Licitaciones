using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Licitaciones.Web.Filtros;

/// <summary>
/// Deja el tipo de cambio activo a disposición de todas las vistas.
/// </summary>
/// <remarks>
/// La alternancia CRC/USD ocurre en el navegador sobre valores ya calculados en el servidor. Este filtro
/// resuelve el tipo de cambio una sola vez por petición, en lugar de que cada vista o cada fila de una
/// tabla lo consulte por su cuenta.
/// </remarks>
public sealed class FiltroTipoCambioActivo : IAsyncActionFilter
{
    /// <summary>Clave de <c>ViewData</c> con el valor de colones por dólar.</summary>
    public const string ClaveCrcPorUsd = "CrcPorUsd";

    /// <summary>Clave de <c>ViewData</c> con la fecha de vigencia del tipo de cambio.</summary>
    public const string ClaveFechaTipoCambio = "FechaTipoCambio";

    private readonly IServicioTiposCambio _servicio;

    /// <summary>
    /// Crea el filtro.
    /// </summary>
    /// <param name="servicio">Casos de uso de tipos de cambio.</param>
    public FiltroTipoCambioActivo(IServicioTiposCambio servicio) => _servicio = servicio;

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (context.Controller is Microsoft.AspNetCore.Mvc.Controller controlador)
        {
            TipoCambioDto? activo = await _servicio.ObtenerActivoAsync(context.HttpContext.RequestAborted);

            if (activo is not null)
            {
                controlador.ViewData[ClaveCrcPorUsd] = activo.CrcPorUsd;
                controlador.ViewData[ClaveFechaTipoCambio] = activo.FechaVigencia;
            }
        }

        await next();
    }
}
