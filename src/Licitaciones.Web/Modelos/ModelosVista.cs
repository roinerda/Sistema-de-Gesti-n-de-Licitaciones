using System.ComponentModel.DataAnnotations;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Entidades;

namespace Licitaciones.Web.Modelos;

/// <summary>
/// Metadatos de paginación que consume la vista parcial de navegación entre páginas.
/// </summary>
/// <param name="Pagina">Página actual.</param>
/// <param name="TotalPaginas">Cantidad total de páginas.</param>
/// <param name="TotalElementos">Cantidad total de elementos.</param>
/// <param name="Accion">Acción a la que apuntan los enlaces.</param>
public sealed record PaginacionVista(int Pagina, int TotalPaginas, int TotalElementos, string Accion);

/// <summary>
/// Formulario de licitación.
/// </summary>
/// <remarks>
/// La fecha se captura como hora local de Costa Rica, que es lo que el control de calendario del navegador
/// envía. La conversión a UTC ocurre aquí y no en el modelo de dominio: si se enlazara directamente a un
/// <see cref="DateTimeOffset"/>, el servidor interpretaría la hora con su propio desplazamiento —UTC dentro
/// del contenedor— y desplazaría la fecha de cierre seis horas.
/// </remarks>
public sealed class LicitacionFormularioVista
{
    /// <summary>Código único de la licitación.</summary>
    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(Licitacion.LongitudMaximaCodigo, ErrorMessage = "El código no puede superar {1} caracteres.")]
    [Display(Name = "Código")]
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Título descriptivo.</summary>
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(Licitacion.LongitudMaximaTitulo, ErrorMessage = "El título no puede superar {1} caracteres.")]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = string.Empty;

    /// <summary>Fecha y hora de cierre, en hora de Costa Rica.</summary>
    [Required(ErrorMessage = "La fecha y hora de cierre es obligatoria.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Fecha y hora de cierre")]
    public DateTime FechaCierreLocal { get; set; }

    /// <summary>Presupuesto estimado en colones.</summary>
    [Required(ErrorMessage = "El presupuesto estimado es obligatorio.")]
    [Range(0.01, 9_999_999_999_999.99, ErrorMessage = "El presupuesto debe ser mayor que cero.")]
    [Display(Name = "Presupuesto estimado (CRC)")]
    public decimal PresupuestoEstimadoCrc { get; set; }

    /// <summary>Versión que el formulario tenía a la vista, para detectar ediciones concurrentes.</summary>
    public int? Version { get; set; }

    /// <summary>Convierte el formulario al DTO de la capa de aplicación.</summary>
    /// <returns>DTO listo para el caso de uso.</returns>
    public GuardarLicitacionDto AGuardar() => new()
    {
        Codigo = Codigo,
        Titulo = Titulo,
        FechaCierre = ZonaHorariaCostaRica.DesdeHoraLocal(FechaCierreLocal),
        PresupuestoEstimadoCrc = PresupuestoEstimadoCrc,
        Version = Version,
    };

    /// <summary>Construye el formulario a partir de una licitación existente.</summary>
    /// <param name="licitacion">Licitación a editar.</param>
    /// <returns>Formulario con los valores actuales.</returns>
    public static LicitacionFormularioVista Desde(LicitacionDto licitacion)
    {
        ArgumentNullException.ThrowIfNull(licitacion);

        return new LicitacionFormularioVista
        {
            Codigo = licitacion.Codigo,
            Titulo = licitacion.Titulo,
            FechaCierreLocal = ZonaHorariaCostaRica.AHoraLocal(licitacion.FechaCierre).DateTime,
            PresupuestoEstimadoCrc = licitacion.PresupuestoEstimadoCrc,
            Version = licitacion.Version,
        };
    }
}

/// <summary>
/// Formulario de tipo de cambio, con la fecha de vigencia expresada en hora de Costa Rica.
/// </summary>
public sealed class TipoCambioFormularioVista
{
    /// <summary>Colones por dólar estadounidense.</summary>
    [Required(ErrorMessage = "El tipo de cambio es obligatorio.")]
    [Range(0.0001, 9_999_999.9999, ErrorMessage = "El tipo de cambio debe ser mayor que cero.")]
    [Display(Name = "Colones por dólar")]
    public decimal CrcPorUsd { get; set; }

    /// <summary>Fecha de vigencia, en hora de Costa Rica.</summary>
    [Required(ErrorMessage = "La fecha de vigencia es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de vigencia")]
    public DateTime FechaVigenciaLocal { get; set; }

    /// <summary>Indica si debe quedar como tipo de cambio activo.</summary>
    [Display(Name = "Marcar como activo")]
    public bool Activo { get; set; }

    /// <summary>Versión que el formulario tenía a la vista.</summary>
    public int? Version { get; set; }

    /// <summary>Convierte el formulario al DTO de la capa de aplicación.</summary>
    /// <returns>DTO listo para el caso de uso.</returns>
    public GuardarTipoCambioDto AGuardar() => new()
    {
        CrcPorUsd = CrcPorUsd,
        FechaVigencia = ZonaHorariaCostaRica.DesdeHoraLocal(FechaVigenciaLocal),
        Activo = Activo,
        Version = Version,
    };

    /// <summary>Construye el formulario a partir de un tipo de cambio existente.</summary>
    /// <param name="tipoCambio">Tipo de cambio a editar.</param>
    /// <returns>Formulario con los valores actuales.</returns>
    public static TipoCambioFormularioVista Desde(TipoCambioDto tipoCambio)
    {
        ArgumentNullException.ThrowIfNull(tipoCambio);

        return new TipoCambioFormularioVista
        {
            CrcPorUsd = tipoCambio.CrcPorUsd,
            FechaVigenciaLocal = ZonaHorariaCostaRica.AHoraLocal(tipoCambio.FechaVigencia).Date,
            Activo = tipoCambio.Activo,
            Version = tipoCambio.Version,
        };
    }
}

/// <summary>
/// Datos de la página inicial.
/// </summary>
public sealed class InicioVista
{
    /// <summary>Cantidad total de licitaciones registradas.</summary>
    public int TotalLicitaciones { get; init; }

    /// <summary>Cantidad total de proveedores vigentes.</summary>
    public int TotalProveedores { get; init; }

    /// <summary>Licitaciones publicadas más próximas a cerrar.</summary>
    public IReadOnlyList<LicitacionDto> LicitacionesPublicadas { get; init; } = [];
}

/// <summary>
/// Datos de la página de error.
/// </summary>
public sealed class ErrorVista
{
    /// <summary>Identificador de la solicitud, útil para localizarla en el registro del servidor.</summary>
    public string? IdentificadorSolicitud { get; init; }

    /// <summary>Indica si hay un identificador que mostrar.</summary>
    public bool MostrarIdentificador => !string.IsNullOrEmpty(IdentificadorSolicitud);
}

/// <summary>
/// Formulario de registro o edición de una oferta, con las listas necesarias para los selectores.
/// </summary>
public sealed class OfertaFormularioVista
{
    /// <summary>Identificador de la oferta cuando se está editando.</summary>
    public Guid? Id { get; init; }

    /// <summary>Datos de la oferta.</summary>
    public GuardarOfertaDto Datos { get; init; } = new();

    /// <summary>Licitaciones que aceptan ofertas.</summary>
    public IReadOnlyList<LicitacionDto> Licitaciones { get; init; } = [];

    /// <summary>Proveedores vigentes.</summary>
    public IReadOnlyList<ProveedorDto> Proveedores { get; init; } = [];

    /// <summary>Código de la licitación cuando la oferta ya existe.</summary>
    public string LicitacionCodigo { get; init; } = string.Empty;

    /// <summary>Nombre del proveedor cuando la oferta ya existe.</summary>
    public string ProveedorNombre { get; init; } = string.Empty;
}
