using System.ComponentModel.DataAnnotations;
using Licitaciones.Domain.Entidades;

namespace Licitaciones.Application.Dtos;

/// <summary>
/// Representación de lectura de un nivel de aprobación.
/// </summary>
/// <param name="Id">Identificador generado por el sistema.</param>
/// <param name="MontoMinimoCrc">Monto mínimo del rango, inclusive.</param>
/// <param name="MontoMaximoCrc">Monto máximo del rango o <see langword="null"/> si es abierto.</param>
/// <param name="Aprobador">Instancia responsable de aprobar.</param>
/// <param name="Version">Testigo de concurrencia optimista.</param>
/// <param name="CreatedAt">Instante de creación en UTC.</param>
/// <param name="UpdatedAt">Instante de última modificación en UTC.</param>
public sealed record NivelAprobacionDto(
    Guid Id,
    decimal MontoMinimoCrc,
    decimal? MontoMaximoCrc,
    string Aprobador,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>Indica si el rango carece de límite superior.</summary>
    public bool EsRangoAbierto => MontoMaximoCrc is null;

    /// <summary>
    /// Proyecta una entidad de dominio al DTO de lectura.
    /// </summary>
    /// <param name="nivel">Entidad de origen.</param>
    /// <returns>DTO equivalente.</returns>
    public static NivelAprobacionDto Desde(NivelAprobacion nivel)
    {
        ArgumentNullException.ThrowIfNull(nivel);
        return new NivelAprobacionDto(
            nivel.Id,
            nivel.MontoMinimoCrc,
            nivel.MontoMaximoCrc,
            nivel.Aprobador,
            nivel.Version,
            nivel.CreatedAt,
            nivel.UpdatedAt);
    }
}

/// <summary>
/// Datos de entrada para crear o editar un nivel de aprobación.
/// </summary>
public sealed class GuardarNivelAprobacionDto
{
    /// <summary>Monto mínimo del rango, inclusive.</summary>
    [Required(ErrorMessage = "El monto mínimo es obligatorio.")]
    [Range(0.01, 9_999_999_999_999.99, ErrorMessage = "El monto mínimo debe ser mayor que cero.")]
    [Display(Name = "Monto mínimo (CRC)")]
    public decimal MontoMinimoCrc { get; set; }

    /// <summary>Monto máximo del rango o <see langword="null"/> para un rango abierto.</summary>
    [Range(0.01, 9_999_999_999_999.99, ErrorMessage = "El monto máximo debe ser mayor que cero.")]
    [Display(Name = "Monto máximo (CRC)")]
    public decimal? MontoMaximoCrc { get; set; }

    /// <summary>Instancia responsable de aprobar.</summary>
    [Required(ErrorMessage = "El aprobador es obligatorio.")]
    [StringLength(NivelAprobacion.LongitudMaximaAprobador, ErrorMessage = "El aprobador no puede superar {1} caracteres.")]
    [Display(Name = "Aprobador")]
    public string Aprobador { get; set; } = string.Empty;

    /// <summary>
    /// Versión que el cliente tenía a la vista. Se envía al editar para detectar ediciones concurrentes.
    /// </summary>
    public int? Version { get; set; }
}

/// <summary>
/// Representación de lectura de un tipo de cambio.
/// </summary>
/// <param name="Id">Identificador generado por el sistema.</param>
/// <param name="CrcPorUsd">Colones por dólar estadounidense.</param>
/// <param name="FechaVigencia">Fecha a partir de la cual rige.</param>
/// <param name="Activo">Indica si es el tipo de cambio vigente.</param>
/// <param name="Version">Testigo de concurrencia optimista.</param>
/// <param name="CreatedAt">Instante de creación en UTC.</param>
/// <param name="UpdatedAt">Instante de última modificación en UTC.</param>
public sealed record TipoCambioDto(
    Guid Id,
    decimal CrcPorUsd,
    DateTimeOffset FechaVigencia,
    bool Activo,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>
    /// Proyecta una entidad de dominio al DTO de lectura.
    /// </summary>
    /// <param name="tipoCambio">Entidad de origen.</param>
    /// <returns>DTO equivalente.</returns>
    public static TipoCambioDto Desde(TipoCambio tipoCambio)
    {
        ArgumentNullException.ThrowIfNull(tipoCambio);
        return new TipoCambioDto(
            tipoCambio.Id,
            tipoCambio.CrcPorUsd,
            tipoCambio.FechaVigencia,
            tipoCambio.Activo,
            tipoCambio.Version,
            tipoCambio.CreatedAt,
            tipoCambio.UpdatedAt);
    }
}

/// <summary>
/// Datos de entrada para crear o editar un tipo de cambio.
/// </summary>
public sealed class GuardarTipoCambioDto
{
    /// <summary>Colones por dólar estadounidense. Debe ser mayor que cero.</summary>
    [Required(ErrorMessage = "El tipo de cambio es obligatorio.")]
    [Range(0.0001, 9_999_999.9999, ErrorMessage = "El tipo de cambio debe ser mayor que cero.")]
    [Display(Name = "Colones por dólar")]
    public decimal CrcPorUsd { get; set; }

    /// <summary>Fecha a partir de la cual rige.</summary>
    [Required(ErrorMessage = "La fecha de vigencia es obligatoria.")]
    [Display(Name = "Fecha de vigencia")]
    public DateTimeOffset FechaVigencia { get; set; }

    /// <summary>Indica si debe quedar como tipo de cambio activo.</summary>
    [Display(Name = "Activo")]
    public bool Activo { get; set; }

    /// <summary>
    /// Versión que el cliente tenía a la vista. Se envía al editar para detectar ediciones concurrentes.
    /// </summary>
    public int? Version { get; set; }
}

/// <summary>
/// Monto expresado simultáneamente en colones y en dólares.
/// </summary>
/// <param name="MontoCrc">Monto oficial en colones.</param>
/// <param name="MontoUsd">Equivalente calculado en dólares.</param>
/// <param name="CrcPorUsd">Tipo de cambio aplicado.</param>
/// <param name="FechaTipoCambio">Fecha de vigencia del tipo de cambio aplicado.</param>
public sealed record MontoConvertidoDto(
    decimal MontoCrc,
    decimal MontoUsd,
    decimal CrcPorUsd,
    DateTimeOffset FechaTipoCambio);
