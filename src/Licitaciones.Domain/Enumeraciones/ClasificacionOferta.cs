namespace Licitaciones.Domain.Enumeraciones;

/// <summary>
/// Clasificación de la mejor oferta de una licitación según el ahorro obtenido (sección 8.6).
/// </summary>
public enum ClasificacionOferta
{
    /// <summary>La licitación no tiene ofertas válidas registradas.</summary>
    SinOfertasValidas = 0,

    /// <summary>Ahorro igual o superior al 10 %.</summary>
    OfertaConveniente = 1,

    /// <summary>Ahorro mayor que 0 % y menor que 10 %.</summary>
    OfertaAceptable = 2,

    /// <summary>La mejor oferta es igual al presupuesto estimado: no hay ahorro.</summary>
    OfertaValidaSinAhorro = 3,
}

/// <summary>
/// Traduce <see cref="ClasificacionOferta"/> al texto exacto exigido por el enunciado.
/// </summary>
/// <remarks>
/// El texto se mantiene aquí, junto a la enumeración, para que la interfaz web, la API y las pruebas
/// usen exactamente la misma redacción sin duplicar literales.
/// </remarks>
public static class ClasificacionOfertaExtensiones
{
    /// <summary>
    /// Devuelve la descripción legible de la clasificación.
    /// </summary>
    /// <param name="clasificacion">Clasificación calculada para la mejor oferta.</param>
    /// <returns>Texto exacto definido en la sección 8.6 del enunciado.</returns>
    public static string Descripcion(this ClasificacionOferta clasificacion) => clasificacion switch
    {
        ClasificacionOferta.SinOfertasValidas => "Sin ofertas válidas",
        ClasificacionOferta.OfertaConveniente => "Oferta conveniente",
        ClasificacionOferta.OfertaAceptable => "Oferta aceptable",
        ClasificacionOferta.OfertaValidaSinAhorro => "Oferta válida sin ahorro",
        _ => throw new ArgumentOutOfRangeException(nameof(clasificacion), clasificacion, "Clasificación desconocida."),
    };
}
