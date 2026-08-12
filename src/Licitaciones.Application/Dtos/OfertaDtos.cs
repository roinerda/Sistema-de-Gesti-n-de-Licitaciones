using System.ComponentModel.DataAnnotations;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Enumeraciones;

namespace Licitaciones.Application.Dtos;

/// <summary>
/// Representación de lectura de una oferta.
/// </summary>
/// <param name="Id">Identificador generado por el sistema.</param>
/// <param name="LicitacionId">Licitación asociada.</param>
/// <param name="LicitacionCodigo">Código de la licitación asociada.</param>
/// <param name="ProveedorId">Proveedor que ofertó.</param>
/// <param name="ProveedorNombre">Nombre del proveedor que ofertó.</param>
/// <param name="MontoOfertadoCrc">Monto ofertado en colones.</param>
/// <param name="FechaRegistro">Instante de registro en UTC.</param>
/// <param name="Version">Testigo de concurrencia optimista.</param>
/// <param name="UpdatedAt">Instante de última modificación en UTC.</param>
public sealed record OfertaDto(
    Guid Id,
    Guid LicitacionId,
    string LicitacionCodigo,
    Guid ProveedorId,
    string ProveedorNombre,
    decimal MontoOfertadoCrc,
    DateTimeOffset FechaRegistro,
    int Version,
    DateTimeOffset UpdatedAt)
{
    /// <summary>
    /// Proyecta una entidad de dominio al DTO de lectura.
    /// </summary>
    /// <param name="oferta">Entidad de origen, preferiblemente con sus navegaciones cargadas.</param>
    /// <returns>DTO equivalente.</returns>
    public static OfertaDto Desde(Oferta oferta)
    {
        ArgumentNullException.ThrowIfNull(oferta);
        return new OfertaDto(
            oferta.Id,
            oferta.LicitacionId,
            oferta.Licitacion?.Codigo ?? string.Empty,
            oferta.ProveedorId,
            oferta.Proveedor?.Nombre ?? string.Empty,
            oferta.MontoOfertadoCrc,
            oferta.FechaRegistro,
            oferta.Version,
            oferta.UpdatedAt);
    }
}

/// <summary>
/// Resultado de evaluar las ofertas de una licitación (mejor oferta, ahorro, clasificación y aprobador).
/// </summary>
/// <param name="LicitacionId">Licitación evaluada.</param>
/// <param name="LicitacionCodigo">Código de la licitación evaluada.</param>
/// <param name="PresupuestoEstimadoCrc">Presupuesto estimado en colones.</param>
/// <param name="Oferta">Mejor oferta, o <see langword="null"/> si no hay ofertas válidas.</param>
/// <param name="PorcentajeAhorro">Ahorro respecto del presupuesto, en porcentaje.</param>
/// <param name="Clasificacion">Clasificación del ahorro.</param>
/// <param name="ClasificacionDescripcion">Texto de la clasificación.</param>
/// <param name="Aprobador">Instancia aprobadora según el monto de la mejor oferta.</param>
public sealed record MejorOfertaDto(
    Guid LicitacionId,
    string LicitacionCodigo,
    decimal PresupuestoEstimadoCrc,
    OfertaDto? Oferta,
    decimal PorcentajeAhorro,
    ClasificacionOferta Clasificacion,
    string ClasificacionDescripcion,
    string? Aprobador);

/// <summary>
/// Datos de entrada para registrar una oferta dentro de una licitación conocida.
/// </summary>
public sealed class CrearOfertaDto
{
    /// <summary>Proveedor que presenta la oferta.</summary>
    [Required(ErrorMessage = "El proveedor es obligatorio.")]
    [Display(Name = "Proveedor")]
    public Guid ProveedorId { get; set; }

    /// <summary>Monto ofertado en colones. Debe ser mayor que cero.</summary>
    [Required(ErrorMessage = "El monto ofertado es obligatorio.")]
    [Range(0.01, 9_999_999_999_999.99, ErrorMessage = "El monto ofertado debe ser mayor que cero.")]
    [Display(Name = "Monto ofertado (CRC)")]
    public decimal MontoOfertadoCrc { get; set; }
}

/// <summary>
/// Datos de entrada para registrar una oferta indicando también la licitación.
/// </summary>
public sealed class GuardarOfertaDto
{
    /// <summary>Licitación a la que se oferta.</summary>
    [Required(ErrorMessage = "La licitación es obligatoria.")]
    [Display(Name = "Licitación")]
    public Guid LicitacionId { get; set; }

    /// <summary>Proveedor que presenta la oferta.</summary>
    [Required(ErrorMessage = "El proveedor es obligatorio.")]
    [Display(Name = "Proveedor")]
    public Guid ProveedorId { get; set; }

    /// <summary>Monto ofertado en colones. Debe ser mayor que cero.</summary>
    [Required(ErrorMessage = "El monto ofertado es obligatorio.")]
    [Range(0.01, 9_999_999_999_999.99, ErrorMessage = "El monto ofertado debe ser mayor que cero.")]
    [Display(Name = "Monto ofertado (CRC)")]
    public decimal MontoOfertadoCrc { get; set; }

    /// <summary>
    /// Versión que el cliente tenía a la vista. Se envía al editar para detectar ediciones concurrentes.
    /// </summary>
    public int? Version { get; set; }
}

/// <summary>
/// Datos de entrada para modificar el monto de una oferta existente.
/// </summary>
public sealed class ActualizarOfertaDto
{
    /// <summary>Nuevo monto ofertado en colones.</summary>
    [Required(ErrorMessage = "El monto ofertado es obligatorio.")]
    [Range(0.01, 9_999_999_999_999.99, ErrorMessage = "El monto ofertado debe ser mayor que cero.")]
    [Display(Name = "Monto ofertado (CRC)")]
    public decimal MontoOfertadoCrc { get; set; }

    /// <summary>
    /// Versión que el cliente tenía a la vista. Se envía al editar para detectar ediciones concurrentes.
    /// </summary>
    public int? Version { get; set; }
}
