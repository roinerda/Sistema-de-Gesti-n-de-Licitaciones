using System.Text.RegularExpressions;

namespace Licitaciones.Domain.Normalizacion;

/// <summary>
/// Expresiones regulares compiladas usadas por las validaciones de dominio.
/// </summary>
/// <remarks>
/// Se generan en tiempo de compilación con <see cref="GeneratedRegexAttribute"/> para evitar el costo
/// de interpretarlas en tiempo de ejecución. La misma expresión se reutiliza en la validación de cliente
/// (atributo <c>pattern</c> del formulario) para que interfaz y servidor coincidan.
/// </remarks>
public static partial class PatronesTexto
{
    /// <summary>
    /// Patrón, en formato compatible con HTML y .NET, de los caracteres permitidos en el nombre de un
    /// proveedor: letras, números, espacios, punto, coma y paréntesis (sección 8.4).
    /// </summary>
    public const string NombreProveedorPatron = @"^[\p{L}\p{N} .,\(\)]+$";

    /// <summary>
    /// Expresión regular compilada que valida el nombre de un proveedor.
    /// </summary>
    /// <returns>Instancia reutilizable de la expresión regular.</returns>
    [GeneratedRegex(NombreProveedorPatron, RegexOptions.CultureInvariant)]
    public static partial Regex NombreProveedor();
}
