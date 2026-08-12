using Licitaciones.Domain.Enumeraciones;

namespace Licitaciones.Domain.Servicios;

/// <summary>
/// Tabla de transiciones permitidas del ciclo de vida de una licitación (sección 8.1).
/// </summary>
/// <remarks>
/// Se modela como un conjunto de pares en lugar de una cadena de <c>if</c> anidados: agregar o quitar una
/// transición es un cambio de datos, no de lógica, y las pruebas pueden recorrer la matriz completa.
/// </remarks>
public static class TransicionesLicitacion
{
    private static readonly HashSet<(EstadoLicitacion Origen, EstadoLicitacion Destino)> Permitidas =
    [
        (EstadoLicitacion.Borrador, EstadoLicitacion.Publicada),
        // Borrador -> Cerrada se admite como cancelación documentada.
        (EstadoLicitacion.Borrador, EstadoLicitacion.Cerrada),
        (EstadoLicitacion.Publicada, EstadoLicitacion.Cerrada),
    ];

    /// <summary>
    /// Indica si la transición entre dos estados está permitida.
    /// </summary>
    /// <param name="origen">Estado actual.</param>
    /// <param name="destino">Estado propuesto.</param>
    /// <returns><see langword="true"/> si la transición es válida.</returns>
    public static bool EsPermitida(EstadoLicitacion origen, EstadoLicitacion destino) =>
        Permitidas.Contains((origen, destino));

    /// <summary>
    /// Devuelve los estados a los que se puede pasar desde el estado indicado.
    /// </summary>
    /// <param name="origen">Estado actual.</param>
    /// <returns>Colección de estados destino válidos, posiblemente vacía.</returns>
    public static IReadOnlyCollection<EstadoLicitacion> DestinosDesde(EstadoLicitacion origen) =>
        Permitidas.Where(t => t.Origen == origen).Select(t => t.Destino).ToArray();
}
