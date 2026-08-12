using Licitaciones.Domain.Enumeraciones;

namespace Licitaciones.Application.Comun;

/// <summary>
/// Parámetros comunes de listado: paginación, búsqueda y ordenamiento.
/// </summary>
/// <remarks>
/// Los valores se saturan dentro de límites razonables para que un cliente no pueda solicitar
/// una página de tamaño arbitrario y degradar la base de datos.
/// </remarks>
public class ParametrosConsulta
{
    /// <summary>Tamaño de página máximo permitido.</summary>
    public const int TamanoPaginaMaximo = 100;

    /// <summary>Tamaño de página por omisión.</summary>
    public const int TamanoPaginaPorOmision = 10;

    private int _pagina = 1;
    private int _tamanoPagina = TamanoPaginaPorOmision;

    /// <summary>Número de página solicitado, empezando en 1.</summary>
    public int Pagina
    {
        get => _pagina;
        set => _pagina = value < 1 ? 1 : value;
    }

    /// <summary>Cantidad de elementos por página.</summary>
    public int TamanoPagina
    {
        get => _tamanoPagina;
        set => _tamanoPagina = value switch
        {
            < 1 => TamanoPaginaPorOmision,
            > TamanoPaginaMaximo => TamanoPaginaMaximo,
            _ => value,
        };
    }

    /// <summary>Texto libre para filtrar los resultados.</summary>
    public string? Buscar { get; set; }

    /// <summary>Campo por el cual ordenar; cada repositorio define los valores admitidos.</summary>
    public string? OrdenarPor { get; set; }

    /// <summary>Indica si el ordenamiento es descendente.</summary>
    public bool Descendente { get; set; }

    /// <summary>Cantidad de elementos que deben omitirse para llegar a la página solicitada.</summary>
    public int Omitir => (Pagina - 1) * TamanoPagina;
}

/// <summary>
/// Parámetros de listado de proveedores.
/// </summary>
public sealed class ParametrosConsultaProveedores : ParametrosConsulta
{
    /// <summary>Incluye en el listado los proveedores dados de baja lógicamente.</summary>
    public bool IncluirEliminados { get; set; }
}

/// <summary>
/// Parámetros de listado de licitaciones.
/// </summary>
public sealed class ParametrosConsultaLicitaciones : ParametrosConsulta
{
    /// <summary>Filtra por estado exacto.</summary>
    public EstadoLicitacion? Estado { get; set; }

    /// <summary>Incluye en el listado las licitaciones dadas de baja lógicamente.</summary>
    public bool IncluirEliminadas { get; set; }
}

/// <summary>
/// Parámetros de listado de ofertas.
/// </summary>
public sealed class ParametrosConsultaOfertas : ParametrosConsulta
{
    /// <summary>Filtra las ofertas de una licitación.</summary>
    public Guid? LicitacionId { get; set; }

    /// <summary>Filtra las ofertas de un proveedor.</summary>
    public Guid? ProveedorId { get; set; }
}
