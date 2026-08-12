namespace Licitaciones.Application.Comun;

/// <summary>
/// Página de resultados de un listado.
/// </summary>
/// <typeparam name="T">Tipo de los elementos devueltos.</typeparam>
public sealed class PaginaResultado<T>
{
    /// <summary>
    /// Crea una página de resultados.
    /// </summary>
    /// <param name="elementos">Elementos de la página actual.</param>
    /// <param name="pagina">Número de página, empezando en 1.</param>
    /// <param name="tamanoPagina">Cantidad de elementos por página.</param>
    /// <param name="totalElementos">Total de elementos que cumplen el filtro.</param>
    public PaginaResultado(IReadOnlyList<T> elementos, int pagina, int tamanoPagina, int totalElementos)
    {
        Elementos = elementos;
        Pagina = pagina;
        TamanoPagina = tamanoPagina;
        TotalElementos = totalElementos;
    }

    /// <summary>Elementos de la página actual.</summary>
    public IReadOnlyList<T> Elementos { get; }

    /// <summary>Número de página, empezando en 1.</summary>
    public int Pagina { get; }

    /// <summary>Cantidad de elementos por página.</summary>
    public int TamanoPagina { get; }

    /// <summary>Total de elementos que cumplen el filtro, sin paginar.</summary>
    public int TotalElementos { get; }

    /// <summary>Cantidad total de páginas disponibles.</summary>
    public int TotalPaginas => TamanoPagina <= 0 ? 0 : (int)Math.Ceiling(TotalElementos / (double)TamanoPagina);

    /// <summary>Indica si existe una página anterior.</summary>
    public bool TienePaginaAnterior => Pagina > 1;

    /// <summary>Indica si existe una página siguiente.</summary>
    public bool TienePaginaSiguiente => Pagina < TotalPaginas;

    /// <summary>
    /// Proyecta los elementos de la página conservando los metadatos de paginación.
    /// </summary>
    /// <typeparam name="TDestino">Tipo destino de la proyección.</typeparam>
    /// <param name="proyeccion">Función de transformación de cada elemento.</param>
    /// <returns>Página equivalente con los elementos transformados.</returns>
    public PaginaResultado<TDestino> Proyectar<TDestino>(Func<T, TDestino> proyeccion)
    {
        ArgumentNullException.ThrowIfNull(proyeccion);
        return new PaginaResultado<TDestino>(
            Elementos.Select(proyeccion).ToArray(),
            Pagina,
            TamanoPagina,
            TotalElementos);
    }

    /// <summary>
    /// Crea una página vacía con los metadatos indicados.
    /// </summary>
    /// <param name="parametros">Parámetros de la consulta original.</param>
    /// <returns>Página sin elementos.</returns>
    public static PaginaResultado<T> Vacia(ParametrosConsulta parametros)
    {
        ArgumentNullException.ThrowIfNull(parametros);
        return new PaginaResultado<T>([], parametros.Pagina, parametros.TamanoPagina, 0);
    }
}
