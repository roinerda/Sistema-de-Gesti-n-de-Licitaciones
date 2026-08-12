using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Excepciones;

namespace Licitaciones.Application.Comun;

/// <summary>
/// Naturaleza de un error de aplicación. Determina el código HTTP en la API y el tratamiento en la web.
/// </summary>
public enum TipoError
{
    /// <summary>Datos de entrada inválidos.</summary>
    Validacion,

    /// <summary>El recurso solicitado no existe.</summary>
    NoEncontrado,

    /// <summary>El estado actual del sistema impide la operación (duplicados, dependencias).</summary>
    Conflicto,

    /// <summary>Una regla de negocio prohíbe la operación.</summary>
    ReglaNegocio,

    /// <summary>Otro proceso modificó el registro antes de guardar los cambios.</summary>
    Concurrencia,
}

/// <summary>
/// Error de aplicación con código estable, mensaje seguro y campo asociado.
/// </summary>
/// <param name="Codigo">Código estable definido en <see cref="CodigosError"/>.</param>
/// <param name="Mensaje">Mensaje comprensible para la persona usuaria, sin detalles técnicos.</param>
/// <param name="Tipo">Naturaleza del error.</param>
/// <param name="Campo">Campo del modelo asociado, si corresponde.</param>
public sealed record ErrorApp(string Codigo, string Mensaje, TipoError Tipo, string? Campo = null)
{
    /// <summary>Crea un error de recurso inexistente.</summary>
    /// <param name="mensaje">Mensaje que se mostrará.</param>
    /// <returns>Error de tipo <see cref="TipoError.NoEncontrado"/>.</returns>
    public static ErrorApp NoEncontrado(string mensaje) =>
        new(CodigosError.NoEncontrado, mensaje, TipoError.NoEncontrado);

    /// <summary>Crea un error de conflicto.</summary>
    /// <param name="codigo">Código estable del conflicto.</param>
    /// <param name="mensaje">Mensaje que se mostrará.</param>
    /// <param name="campo">Campo asociado, si aplica.</param>
    /// <returns>Error de tipo <see cref="TipoError.Conflicto"/>.</returns>
    public static ErrorApp Conflicto(string codigo, string mensaje, string? campo = null) =>
        new(codigo, mensaje, TipoError.Conflicto, campo);

    /// <summary>Crea un error de validación de entrada.</summary>
    /// <param name="mensaje">Mensaje que se mostrará.</param>
    /// <param name="campo">Campo asociado, si aplica.</param>
    /// <returns>Error de tipo <see cref="TipoError.Validacion"/>.</returns>
    public static ErrorApp Validacion(string mensaje, string? campo = null) =>
        new(CodigosError.ValidacionEntrada, mensaje, TipoError.Validacion, campo);

    /// <summary>Crea un error de concurrencia optimista.</summary>
    /// <returns>Error de tipo <see cref="TipoError.Concurrencia"/>.</returns>
    public static ErrorApp Concurrencia() =>
        new(CodigosError.Concurrencia,
            "Otro usuario modificó el registro mientras usted trabajaba. Vuelva a cargarlo e intente de nuevo.",
            TipoError.Concurrencia);

    /// <summary>Traduce una excepción de dominio a un error de aplicación.</summary>
    /// <param name="excepcion">Excepción lanzada por el dominio.</param>
    /// <returns>Error de tipo <see cref="TipoError.ReglaNegocio"/>.</returns>
    public static ErrorApp DesdeDominio(ReglaNegocioException excepcion)
    {
        ArgumentNullException.ThrowIfNull(excepcion);
        return new ErrorApp(excepcion.Codigo, excepcion.Message, TipoError.ReglaNegocio, excepcion.Campo);
    }
}

/// <summary>
/// Resultado de una operación sin valor de retorno.
/// </summary>
/// <remarks>
/// Los casos de uso devuelven resultados en lugar de lanzar excepciones para el flujo esperado: así la
/// capa de presentación decide el código HTTP o el mensaje de la vista sin capturar excepciones.
/// </remarks>
public sealed class Resultado
{
    private Resultado(ErrorApp? error) => Error = error;

    /// <summary>Error ocurrido, o <see langword="null"/> si la operación fue exitosa.</summary>
    public ErrorApp? Error { get; }

    /// <summary>Indica si la operación terminó correctamente.</summary>
    public bool EsExito => Error is null;

    /// <summary>Crea un resultado exitoso.</summary>
    /// <returns>Resultado sin error.</returns>
    public static Resultado Exitoso() => new(null);

    /// <summary>Crea un resultado fallido.</summary>
    /// <param name="error">Error que explica la falla.</param>
    /// <returns>Resultado con error.</returns>
    public static Resultado Fallido(ErrorApp error) => new(error);
}

/// <summary>
/// Resultado de una operación que devuelve un valor.
/// </summary>
/// <typeparam name="T">Tipo del valor devuelto en caso de éxito.</typeparam>
public sealed class Resultado<T>
{
    private Resultado(T? valor, ErrorApp? error)
    {
        Valor = valor;
        Error = error;
    }

    /// <summary>Valor producido por la operación, o <see langword="null"/> si falló.</summary>
    public T? Valor { get; }

    /// <summary>Error ocurrido, o <see langword="null"/> si la operación fue exitosa.</summary>
    public ErrorApp? Error { get; }

    /// <summary>Indica si la operación terminó correctamente.</summary>
    public bool EsExito => Error is null;

    /// <summary>Crea un resultado exitoso con su valor.</summary>
    /// <param name="valor">Valor producido.</param>
    /// <returns>Resultado exitoso.</returns>
    public static Resultado<T> Exitoso(T valor) => new(valor, null);

    /// <summary>Crea un resultado fallido.</summary>
    /// <param name="error">Error que explica la falla.</param>
    /// <returns>Resultado con error.</returns>
    public static Resultado<T> Fallido(ErrorApp error) => new(default, error);
}
