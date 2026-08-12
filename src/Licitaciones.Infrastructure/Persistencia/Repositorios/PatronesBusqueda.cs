namespace Licitaciones.Infrastructure.Persistencia.Repositorios;

/// <summary>
/// Construcción segura de patrones para las búsquedas con <c>LIKE</c> e <c>ILIKE</c>.
/// </summary>
/// <remarks>
/// Los caracteres <c>%</c>, <c>_</c> y <c>\</c> tienen significado especial en PostgreSQL. Si el texto que
/// escribe la persona usuaria los contiene y no se escapan, la búsqueda devuelve resultados incorrectos.
/// </remarks>
internal static class PatronesBusqueda
{
    /// <summary>
    /// Convierte un texto libre en un patrón «contiene» con los comodines escapados.
    /// </summary>
    /// <param name="valor">Texto escrito por la persona usuaria.</param>
    /// <returns>Patrón listo para usar con <c>ILIKE</c>.</returns>
    public static string ParaContiene(string valor)
    {
        string escapado = valor
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

        return $"%{escapado}%";
    }
}
