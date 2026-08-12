using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Licitaciones.Domain.Normalizacion;

/// <summary>
/// Normalización de texto usada para comparar unicidad de nombres y códigos (sección 8.3).
/// </summary>
/// <remarks>
/// La normalización es una regla de negocio, no un detalle de infraestructura: el valor normalizado se
/// persiste en su propia columna con índice único, de modo que la base de datos rechace duplicados aunque
/// alguien evada la validación de interfaz o de servidor.
/// </remarks>
public static partial class NormalizadorTexto
{
    /// <summary>
    /// Normaliza el nombre de un proveedor: recorta espacios laterales, reduce espacios repetidos,
    /// aplica normalización Unicode (FormC) y convierte a mayúsculas invariantes.
    /// </summary>
    /// <param name="valor">Nombre tal como lo escribió la persona usuaria.</param>
    /// <returns>Cadena normalizada para comparación y para el índice único.</returns>
    /// <example>
    /// <c>" Empresa   Central "</c>, <c>"empresa central"</c> y <c>"EMPRESA   CENTRAL"</c>
    /// producen todas <c>"EMPRESA CENTRAL"</c>.
    /// </example>
    public static string NormalizarNombre(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        string colapsado = EspaciosRepetidos().Replace(valor.Trim(), " ");
        return colapsado.Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }

    /// <summary>
    /// Normaliza el código de una licitación: ignora espacios laterales y diferencias de mayúsculas.
    /// </summary>
    /// <param name="valor">Código tal como lo escribió la persona usuaria.</param>
    /// <returns>Cadena normalizada para comparación y para el índice único.</returns>
    public static string NormalizarCodigo(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        return valor.Trim().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }

    /// <summary>
    /// Recorta espacios laterales y colapsa los espacios internos repetidos, sin cambiar mayúsculas.
    /// Es el valor que se muestra al usuario.
    /// </summary>
    /// <param name="valor">Texto original.</param>
    /// <returns>Texto limpio para presentación y almacenamiento.</returns>
    public static string LimpiarEspacios(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        return EspaciosRepetidos().Replace(valor.Trim(), " ");
    }

    /// <summary>
    /// Formatea un monto en la cultura es-CR sin el símbolo de moneda.
    /// </summary>
    /// <param name="monto">Monto a formatear.</param>
    /// <returns>Representación con separadores de miles y dos decimales.</returns>
    public static string FormatearMonto(decimal monto) =>
        monto.ToString("N2", CultureInfo.GetCultureInfo("es-CR"));

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex EspaciosRepetidos();
}
