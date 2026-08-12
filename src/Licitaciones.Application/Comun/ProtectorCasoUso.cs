using Licitaciones.Application.Abstracciones;
using Licitaciones.Domain.Excepciones;

namespace Licitaciones.Application.Comun;

/// <summary>
/// Envoltura común de los casos de uso que traduce excepciones esperadas a resultados tipados.
/// </summary>
/// <remarks>
/// Evita repetir el mismo <c>try/catch</c> en cada servicio. Solo captura las excepciones previstas por
/// el diseño (regla de negocio, unicidad y concurrencia); cualquier otra se propaga para que el
/// middleware de errores la registre y devuelva una respuesta 500 controlada.
/// </remarks>
public static class ProtectorCasoUso
{
    /// <summary>
    /// Ejecuta un caso de uso que devuelve un valor.
    /// </summary>
    /// <typeparam name="T">Tipo del valor devuelto.</typeparam>
    /// <param name="operacion">Caso de uso a ejecutar.</param>
    /// <param name="traducirUnicidad">
    /// Traducción del índice único violado a un error de negocio comprensible.
    /// </param>
    /// <returns>Resultado del caso de uso o el error traducido.</returns>
    public static async Task<Resultado<T>> ProtegerAsync<T>(
        Func<Task<Resultado<T>>> operacion,
        Func<ViolacionUnicidadException, ErrorApp>? traducirUnicidad = null)
    {
        ArgumentNullException.ThrowIfNull(operacion);

        try
        {
            return await operacion();
        }
        catch (ReglaNegocioException excepcion)
        {
            return Resultado<T>.Fallido(ErrorApp.DesdeDominio(excepcion));
        }
        catch (ViolacionUnicidadException excepcion)
        {
            return Resultado<T>.Fallido(traducirUnicidad?.Invoke(excepcion) ?? ErrorUnicidadPorOmision(excepcion));
        }
        catch (ViolacionIntegridadException excepcion)
        {
            return Resultado<T>.Fallido(ErrorApp.Conflicto("CONFLICTO_INTEGRIDAD", excepcion.Message));
        }
        catch (ConflictoConcurrenciaException)
        {
            return Resultado<T>.Fallido(ErrorApp.Concurrencia());
        }
    }

    /// <summary>
    /// Ejecuta un caso de uso sin valor de retorno.
    /// </summary>
    /// <param name="operacion">Caso de uso a ejecutar.</param>
    /// <param name="traducirUnicidad">
    /// Traducción del índice único violado a un error de negocio comprensible.
    /// </param>
    /// <returns>Resultado del caso de uso o el error traducido.</returns>
    public static async Task<Resultado> ProtegerAsync(
        Func<Task<Resultado>> operacion,
        Func<ViolacionUnicidadException, ErrorApp>? traducirUnicidad = null)
    {
        ArgumentNullException.ThrowIfNull(operacion);

        try
        {
            return await operacion();
        }
        catch (ReglaNegocioException excepcion)
        {
            return Resultado.Fallido(ErrorApp.DesdeDominio(excepcion));
        }
        catch (ViolacionUnicidadException excepcion)
        {
            return Resultado.Fallido(traducirUnicidad?.Invoke(excepcion) ?? ErrorUnicidadPorOmision(excepcion));
        }
        catch (ViolacionIntegridadException excepcion)
        {
            return Resultado.Fallido(ErrorApp.Conflicto("CONFLICTO_INTEGRIDAD", excepcion.Message));
        }
        catch (ConflictoConcurrenciaException)
        {
            return Resultado.Fallido(ErrorApp.Concurrencia());
        }
    }

    private static ErrorApp ErrorUnicidadPorOmision(ViolacionUnicidadException excepcion) =>
        ErrorApp.Conflicto("CONFLICTO_UNICIDAD", excepcion.Message);
}
