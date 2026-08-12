using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Excepciones;

namespace Licitaciones.Domain.Servicios;

/// <summary>
/// Conversión referencial de colones a dólares (sección 8.8).
/// </summary>
/// <remarks>
/// La conversión es una representación calculada: los valores oficiales se almacenan únicamente en CRC y
/// nunca se modifican al mostrarlos en USD.
/// </remarks>
public static class ConversorMoneda
{
    /// <summary>
    /// Convierte un monto de colones a dólares usando el tipo de cambio indicado.
    /// </summary>
    /// <param name="montoCrc">Monto en colones.</param>
    /// <param name="crcPorUsd">Colones por dólar; debe ser mayor que cero.</param>
    /// <returns>Monto equivalente en dólares, redondeado a dos decimales.</returns>
    /// <exception cref="ReglaNegocioException">Si el tipo de cambio no es mayor que cero.</exception>
    public static decimal ConvertirACrcAUsd(decimal montoCrc, decimal crcPorUsd)
    {
        if (crcPorUsd <= 0m)
        {
            throw new ReglaNegocioException(
                CodigosError.TipoCambioInvalido,
                "El tipo de cambio debe ser mayor que cero para convertir montos.");
        }

        return decimal.Round(montoCrc / crcPorUsd, 2, MidpointRounding.AwayFromZero);
    }
}
