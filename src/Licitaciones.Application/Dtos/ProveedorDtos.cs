using System.ComponentModel.DataAnnotations;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Normalizacion;

namespace Licitaciones.Application.Dtos;

/// <summary>
/// Representación de lectura de un proveedor.
/// </summary>
/// <param name="Id">Identificador generado por el sistema.</param>
/// <param name="Nombre">Nombre visible.</param>
/// <param name="NombreNormalizado">Nombre normalizado usado para la unicidad.</param>
/// <param name="CantidadOfertas">Cantidad de ofertas relacionadas.</param>
/// <param name="Eliminado">Indica si el proveedor está dado de baja lógicamente.</param>
/// <param name="Version">Testigo de concurrencia optimista.</param>
/// <param name="CreatedAt">Instante de creación en UTC.</param>
/// <param name="UpdatedAt">Instante de última modificación en UTC.</param>
public sealed record ProveedorDto(
    Guid Id,
    string Nombre,
    string NombreNormalizado,
    int CantidadOfertas,
    bool Eliminado,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>
    /// Proyecta una entidad de dominio al DTO de lectura.
    /// </summary>
    /// <param name="proveedor">Entidad de origen.</param>
    /// <param name="cantidadOfertas">Cantidad de ofertas relacionadas, si ya fue calculada.</param>
    /// <returns>DTO equivalente.</returns>
    public static ProveedorDto Desde(Proveedor proveedor, int cantidadOfertas = 0)
    {
        ArgumentNullException.ThrowIfNull(proveedor);
        return new ProveedorDto(
            proveedor.Id,
            proveedor.Nombre,
            proveedor.NombreNormalizado,
            cantidadOfertas,
            proveedor.EstaEliminado,
            proveedor.Version,
            proveedor.CreatedAt,
            proveedor.UpdatedAt);
    }
}

/// <summary>
/// Datos de entrada para crear o editar un proveedor.
/// </summary>
/// <remarks>
/// Las anotaciones replican en el cliente y en el servidor las reglas del dominio (sección 8.4), de modo
/// que la persona usuaria reciba el error junto al campo antes de enviar el formulario.
/// </remarks>
public sealed class GuardarProveedorDto
{
    /// <summary>Nombre del proveedor.</summary>
    [Required(ErrorMessage = "El nombre del proveedor es obligatorio.")]
    [StringLength(Proveedor.LongitudMaximaNombre, ErrorMessage = "El nombre no puede superar {1} caracteres.")]
    [RegularExpression(
        PatronesTexto.NombreProveedorPatron,
        ErrorMessage = "El nombre solo admite letras, números, espacios, punto, coma y paréntesis.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Versión que el cliente tenía a la vista. Se envía al editar para detectar ediciones concurrentes.
    /// </summary>
    public int? Version { get; set; }
}
