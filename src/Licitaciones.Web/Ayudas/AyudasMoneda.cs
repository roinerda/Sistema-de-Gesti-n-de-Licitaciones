using Licitaciones.Application.Comun;
using Licitaciones.Domain.Servicios;
using Licitaciones.Web.Filtros;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Licitaciones.Web.Ayudas;

/// <summary>
/// Presentación de montos con alternancia entre colones y dólares.
/// </summary>
/// <remarks>
/// El servidor emite ambas representaciones y el navegador solo cambia cuál se ve. De esta forma la
/// conversión se calcula una única vez, con el mismo redondeo que usa la API, y el valor oficial en
/// colones nunca se recalcula ni se altera en el cliente.
/// </remarks>
public static class AyudasMoneda
{
    /// <summary>
    /// Genera el marcado de un monto en colones con su equivalente en dólares.
    /// </summary>
    /// <param name="ayuda">Ayudante de vista.</param>
    /// <param name="montoCrc">Monto oficial en colones.</param>
    /// <returns>Marcado con ambas monedas; el navegador muestra la seleccionada.</returns>
    public static IHtmlContent Monto(this IHtmlHelper ayuda, decimal montoCrc)
    {
        ArgumentNullException.ThrowIfNull(ayuda);

        string colones = ZonaHorariaCostaRica.FormatearColones(montoCrc);
        string dolares = "—";

        if (ayuda.ViewData[FiltroTipoCambioActivo.ClaveCrcPorUsd] is decimal crcPorUsd && crcPorUsd > 0m)
        {
            dolares = ZonaHorariaCostaRica.FormatearDolares(ConversorMoneda.ConvertirACrcAUsd(montoCrc, crcPorUsd));
        }

        var constructor = new HtmlContentBuilder();
        constructor.AppendHtml("<span class=\"monto\">");
        constructor.AppendHtml("<span class=\"monto-crc\">");
        constructor.Append(colones);
        constructor.AppendHtml("</span>");
        constructor.AppendHtml("<span class=\"monto-usd d-none\">");
        constructor.Append(dolares);
        constructor.AppendHtml("</span>");
        constructor.AppendHtml("</span>");

        return constructor;
    }

    /// <summary>
    /// Formatea un instante en la hora local de Costa Rica.
    /// </summary>
    /// <param name="ayuda">Ayudante de vista.</param>
    /// <param name="instante">Instante almacenado en UTC.</param>
    /// <returns>Texto con fecha y hora local.</returns>
    public static string FechaLocal(this IHtmlHelper ayuda, DateTimeOffset instante)
    {
        ArgumentNullException.ThrowIfNull(ayuda);
        return ZonaHorariaCostaRica.Formatear(instante);
    }
}
