using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Licitaciones.Api.Comun;

/// <summary>
/// Convierte cualquier excepción no controlada en una respuesta 500 segura.
/// </summary>
/// <remarks>
/// El detalle técnico se registra en el servidor y al cliente solo se le devuelve un mensaje genérico con
/// el identificador de correlación. Así se cumple la prohibición de exponer trazas, rutas internas,
/// consultas o secretos (sección 10.2 del enunciado).
/// </remarks>
public sealed class ManejadorExcepcionesGlobal : IExceptionHandler
{
    private readonly ILogger<ManejadorExcepcionesGlobal> _registro;

    /// <summary>
    /// Crea el manejador.
    /// </summary>
    /// <param name="registro">Registro de eventos.</param>
    public ManejadorExcepcionesGlobal(ILogger<ManejadorExcepcionesGlobal> registro) => _registro = registro;

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        string identificadorCorrelacion = httpContext.TraceIdentifier;

        _registro.LogError(
            exception,
            "Error no controlado en {Metodo} {Ruta}. Correlación: {Correlacion}.",
            httpContext.Request.Method,
            httpContext.Request.Path,
            identificadorCorrelacion);

        var problema = new ProblemDetails
        {
            Title = "Error interno del servidor",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "Ocurrió un error inesperado al procesar la solicitud. " +
                     "Comunique el identificador de correlación al equipo técnico.",
            Instance = httpContext.Request.Path,
        };

        problema.Extensions["codigoError"] = "ERROR_INTERNO";
        problema.Extensions["identificadorCorrelacion"] = identificadorCorrelacion;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problema, cancellationToken);

        return true;
    }
}
