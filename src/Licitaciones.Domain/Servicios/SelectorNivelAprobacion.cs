using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Excepciones;

namespace Licitaciones.Domain.Servicios;

/// <summary>
/// Selección del aprobador a partir de la tabla parametrizable de niveles (sección 8.7).
/// </summary>
public static class SelectorNivelAprobacion
{
    /// <summary>
    /// Busca el nivel de aprobación que contiene el monto indicado.
    /// </summary>
    /// <param name="niveles">Niveles configurados.</param>
    /// <param name="montoCrc">Monto en colones a clasificar.</param>
    /// <returns>Nivel correspondiente, o <see langword="null"/> si ningún rango cubre el monto.</returns>
    public static NivelAprobacion? Seleccionar(IEnumerable<NivelAprobacion> niveles, decimal montoCrc)
    {
        ArgumentNullException.ThrowIfNull(niveles);

        // Los rangos no se traslapan, por lo que a lo sumo uno contiene el monto. Se ordena por
        // monto mínimo para que el resultado sea estable ante cualquier orden de lectura.
        return niveles
            .OrderBy(n => n.MontoMinimoCrc)
            .FirstOrDefault(n => n.Contiene(montoCrc));
    }

    /// <summary>
    /// Valida que un nivel pueda agregarse o modificarse sin traslaparse con los ya existentes y sin
    /// crear un segundo rango abierto.
    /// </summary>
    /// <param name="existentes">Niveles ya configurados, excluyendo el que se está modificando.</param>
    /// <param name="candidato">Nivel propuesto.</param>
    /// <exception cref="ReglaNegocioException">Si hay traslape o más de un rango abierto.</exception>
    public static void GarantizarRangoConsistente(IEnumerable<NivelAprobacion> existentes, NivelAprobacion candidato)
    {
        ArgumentNullException.ThrowIfNull(existentes);
        ArgumentNullException.ThrowIfNull(candidato);

        var otros = existentes.Where(n => n.Id != candidato.Id).ToList();

        if (candidato.EsRangoAbierto && otros.Exists(n => n.EsRangoAbierto))
        {
            throw new ReglaNegocioException(
                CodigosError.RangoAbiertoDuplicado,
                "Solo puede existir un rango sin monto máximo.");
        }

        NivelAprobacion? traslapado = otros.Find(n => n.SeTraslapaCon(candidato));

        if (traslapado is not null)
        {
            throw new ReglaNegocioException(
                CodigosError.RangoAprobacionTraslapado,
                $"El rango propuesto se traslapa con el nivel «{traslapado.Aprobador}».");
        }
    }
}
