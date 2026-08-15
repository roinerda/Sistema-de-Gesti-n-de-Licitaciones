using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Enumeraciones;

namespace Licitaciones.Domain.Servicios;

/// <summary>
/// Resultado de evaluar las ofertas de una licitación.
/// </summary>
/// <param name="MejorOferta">Oferta ganadora, o <see langword="null"/> si no hay ofertas válidas.</param>
/// <param name="PorcentajeAhorro">Ahorro respecto del presupuesto, redondeado a dos decimales.</param>
/// <param name="Clasificacion">Clasificación del ahorro según la sección 8.6.</param>
public sealed record EvaluacionOfertas(Oferta? MejorOferta, decimal PorcentajeAhorro, ClasificacionOferta Clasificacion);

/// <summary>
/// Cálculo de la mejor oferta, el ahorro y su clasificación (sección 8.6).
/// </summary>
public static class EvaluadorOfertas
{
    /// <summary>
    /// Selecciona la mejor oferta: la de menor monto en colones y, en caso de empate, la registrada primero.
    /// </summary>
    /// <param name="ofertas">Ofertas válidas de la licitación.</param>
    /// <returns>La mejor oferta, o <see langword="null"/> si la colección está vacía.</returns>
    public static Oferta? ObtenerMejorOferta(IEnumerable<Oferta> ofertas)
    {
        ArgumentNullException.ThrowIfNull(ofertas);

        // El tercer criterio (Id) solo actúa si dos ofertas comparten monto y marca de tiempo exacta:
        // garantiza que el resultado sea determinista y reproducible en pruebas.
        return ofertas
            .OrderBy(o => o.MontoOfertadoCrc)
            .ThenBy(o => o.FechaRegistro)
            .ThenBy(o => o.Id)
            .FirstOrDefault();
    }

    /// <summary>
    /// Calcula el porcentaje de ahorro respecto del presupuesto estimado.
    /// </summary>
    /// <param name="presupuestoCrc">Presupuesto estimado en colones; debe ser mayor que cero.</param>
    /// <param name="mejorOfertaCrc">Monto de la mejor oferta en colones.</param>
    /// <returns>Porcentaje de ahorro redondeado a dos decimales.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Si el presupuesto no es mayor que cero.</exception>
    public static decimal CalcularPorcentajeAhorro(decimal presupuestoCrc, decimal mejorOfertaCrc) =>
        decimal.Round(CalcularPorcentajeAhorroExacto(presupuestoCrc, mejorOfertaCrc), 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Evalúa las ofertas de una licitación y devuelve mejor oferta, ahorro y clasificación.
    /// </summary>
    /// <param name="presupuestoCrc">Presupuesto estimado en colones.</param>
    /// <param name="ofertas">Ofertas válidas de la licitación.</param>
    /// <returns>Resultado consolidado de la evaluación.</returns>
    public static EvaluacionOfertas Evaluar(decimal presupuestoCrc, IEnumerable<Oferta> ofertas)
    {
        Oferta? mejor = ObtenerMejorOferta(ofertas);

        if (mejor is null)
        {
            return new EvaluacionOfertas(null, 0m, ClasificacionOferta.SinOfertasValidas);
        }

        // La clasificación usa el ahorro exacto y la presentación el ahorro redondeado. Si se clasificara
        // sobre el valor redondeado, un ahorro real pero minúsculo (por ejemplo ₡1 sobre ₡1 000 000) se
        // redondearía a 0,00 % y quedaría marcado como «sin ahorro», contradiciendo la regla del enunciado.
        decimal ahorroExacto = CalcularPorcentajeAhorroExacto(presupuestoCrc, mejor.MontoOfertadoCrc);
        decimal ahorroPresentado = decimal.Round(ahorroExacto, 2, MidpointRounding.AwayFromZero);

        return new EvaluacionOfertas(mejor, ahorroPresentado, Clasificar(ahorroExacto));
    }

    /// <summary>
    /// Clasifica un porcentaje de ahorro.
    /// </summary>
    /// <param name="porcentajeAhorro">Ahorro calculado, en porcentaje.</param>
    /// <returns>Clasificación correspondiente.</returns>
    public static ClasificacionOferta Clasificar(decimal porcentajeAhorro) => porcentajeAhorro switch
    {
        >= 10m => ClasificacionOferta.OfertaConveniente,
        > 0m => ClasificacionOferta.OfertaAceptable,
        _ => ClasificacionOferta.OfertaValidaSinAhorro,
    };

    private static decimal CalcularPorcentajeAhorroExacto(decimal presupuestoCrc, decimal mejorOfertaCrc)
    {
        if (presupuestoCrc <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(presupuestoCrc),
                presupuestoCrc,
                "El presupuesto debe ser mayor que cero para calcular el ahorro.");
        }

        return (presupuestoCrc - mejorOfertaCrc) / presupuestoCrc * 100m;
    }
}
