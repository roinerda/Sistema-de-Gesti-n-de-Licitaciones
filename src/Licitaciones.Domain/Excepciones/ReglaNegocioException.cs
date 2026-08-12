namespace Licitaciones.Domain.Excepciones;

/// <summary>
/// Excepción que señala la violación de una regla de negocio o de un invariante del dominio.
/// </summary>
/// <remarks>
/// El dominio protege sus invariantes lanzando esta excepción; la capa de aplicación la traduce a un
/// resultado tipado y la capa de presentación a un mensaje controlado (<c>ProblemDetails</c> en la API,
/// validación junto al campo en la interfaz web). De esta forma nunca se filtran detalles técnicos.
/// </remarks>
public sealed class ReglaNegocioException : Exception
{
    /// <summary>
    /// Crea la excepción con un código estable y un mensaje apto para el usuario final.
    /// </summary>
    /// <param name="codigo">Código de error definido en <see cref="Comun.CodigosError"/>.</param>
    /// <param name="mensaje">Mensaje comprensible, sin detalles técnicos.</param>
    /// <param name="campo">Nombre del campo asociado, cuando el error corresponde a uno concreto.</param>
    public ReglaNegocioException(string codigo, string mensaje, string? campo = null)
        : base(mensaje)
    {
        Codigo = codigo;
        Campo = campo;
    }

    /// <summary>
    /// Código estable del error, útil para pruebas automatizadas y clientes de la API.
    /// </summary>
    public string Codigo { get; }

    /// <summary>
    /// Campo del modelo al que corresponde el error, si aplica.
    /// </summary>
    public string? Campo { get; }
}
