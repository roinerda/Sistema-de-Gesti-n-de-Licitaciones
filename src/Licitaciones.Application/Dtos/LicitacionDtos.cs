using System.ComponentModel.DataAnnotations;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Enumeraciones;

namespace Licitaciones.Application.Dtos;

/// <summary>
/// Representación de lectura de una licitación para listados.
/// </summary>
/// <param name="Id">Identificador generado por el sistema.</param>
/// <param name="Codigo">Código visible.</param>
/// <param name="CodigoNormalizado">Código normalizado usado para la unicidad.</param>
/// <param name="Titulo">Título descriptivo.</param>
/// <param name="Estado">Estado del ciclo de vida.</param>
/// <param name="FechaCierre">Fecha y hora de cierre en UTC.</param>
/// <param name="PresupuestoEstimadoCrc">Presupuesto estimado en colones.</param>
/// <param name="CantidadOfertas">Cantidad de ofertas registradas.</param>
/// <param name="CerradaFuncionalmente">Indica si ya no admite actividad, por estado o por vencimiento.</param>
/// <param name="Eliminada">Indica si está dada de baja lógicamente.</param>
/// <param name="Version">Testigo de concurrencia optimista.</param>
/// <param name="CreatedAt">Instante de creación en UTC.</param>
/// <param name="UpdatedAt">Instante de última modificación en UTC.</param>
public sealed record LicitacionDto(
    Guid Id,
    string Codigo,
    string CodigoNormalizado,
    string Titulo,
    EstadoLicitacion Estado,
    DateTimeOffset FechaCierre,
    decimal PresupuestoEstimadoCrc,
    int CantidadOfertas,
    bool CerradaFuncionalmente,
    bool Eliminada,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>Nombre del estado, útil para clientes que no interpretan la enumeración.</summary>
    public string EstadoDescripcion => Estado.ToString();

    /// <summary>
    /// Proyecta una entidad de dominio al DTO de listado.
    /// </summary>
    /// <param name="licitacion">Entidad de origen.</param>
    /// <param name="ahora">Instante actual, para calcular el cierre funcional.</param>
    /// <param name="cantidadOfertas">Cantidad de ofertas relacionadas, si ya fue calculada.</param>
    /// <returns>DTO equivalente.</returns>
    public static LicitacionDto Desde(Licitacion licitacion, DateTimeOffset ahora, int cantidadOfertas = 0)
    {
        ArgumentNullException.ThrowIfNull(licitacion);
        return new LicitacionDto(
            licitacion.Id,
            licitacion.Codigo,
            licitacion.CodigoNormalizado,
            licitacion.Titulo,
            licitacion.Estado,
            licitacion.FechaCierre,
            licitacion.PresupuestoEstimadoCrc,
            cantidadOfertas,
            licitacion.EstaCerradaFuncionalmente(ahora),
            licitacion.EstaEliminada,
            licitacion.Version,
            licitacion.CreatedAt,
            licitacion.UpdatedAt);
    }
}

/// <summary>
/// Representación detallada de una licitación, con mejor oferta, clasificación y aprobador.
/// </summary>
/// <param name="Licitacion">Datos generales de la licitación.</param>
/// <param name="MejorOferta">Resultado de la evaluación de ofertas.</param>
/// <param name="TransicionesPermitidas">Estados a los que se puede pasar desde el estado actual.</param>
public sealed record LicitacionDetalleDto(
    LicitacionDto Licitacion,
    MejorOfertaDto MejorOferta,
    IReadOnlyCollection<EstadoLicitacion> TransicionesPermitidas);

/// <summary>
/// Datos de entrada para crear o editar una licitación.
/// </summary>
public sealed class GuardarLicitacionDto
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

    /// <summary>Fecha y hora de cierre. Debe ser futura.</summary>
    [Required(ErrorMessage = "La fecha y hora de cierre es obligatoria.")]
    [Display(Name = "Fecha y hora de cierre")]
    public DateTimeOffset FechaCierre { get; set; }

    /// <summary>Presupuesto estimado en colones. Debe ser mayor que cero.</summary>
    [Required(ErrorMessage = "El presupuesto estimado es obligatorio.")]
    [Range(0.01, 9_999_999_999_999.99, ErrorMessage = "El presupuesto debe ser mayor que cero.")]
    [Display(Name = "Presupuesto estimado (CRC)")]
    public decimal PresupuestoEstimadoCrc { get; set; }

    /// <summary>
    /// Versión que el cliente tenía a la vista. Se envía al editar para detectar ediciones concurrentes.
    /// </summary>
    public int? Version { get; set; }
}

/// <summary>
/// Datos de entrada para cambiar el estado de una licitación.
/// </summary>
public sealed class CambiarEstadoLicitacionDto
{
    /// <summary>Estado destino solicitado.</summary>
    [Required(ErrorMessage = "El nuevo estado es obligatorio.")]
    [Display(Name = "Nuevo estado")]
    public EstadoLicitacion NuevoEstado { get; set; }
}
