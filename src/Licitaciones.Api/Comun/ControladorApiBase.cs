using Licitaciones.Application.Comun;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Comun;

/// <summary>
/// Base de los controladores REST: traduce los resultados de la capa de aplicación a respuestas HTTP.
/// </summary>
/// <remarks>
/// Centralizar la traducción garantiza que todos los endpoints devuelvan los mismos códigos y el mismo
/// formato de error, y mantiene los controladores delgados: no contienen lógica de negocio.
/// </remarks>
[ApiController]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
public abstract class ControladorApiBase : ControllerBase
{
    /// <summary>
    /// Devuelve 200 con el valor, o el problema correspondiente al error.
    /// </summary>
    /// <typeparam name="T">Tipo del valor devuelto.</typeparam>
    /// <param name="resultado">Resultado del caso de uso.</param>
    /// <returns>Respuesta HTTP equivalente.</returns>
    protected ActionResult<T> Responder<T>(Resultado<T> resultado)
    {
        ArgumentNullException.ThrowIfNull(resultado);
        return resultado.EsExito ? Ok(resultado.Valor) : ProblemaDesde(resultado.Error!);
    }

    /// <summary>
    /// Devuelve 204 cuando la operación fue exitosa, o el problema correspondiente al error.
    /// </summary>
    /// <param name="resultado">Resultado del caso de uso.</param>
    /// <returns>Respuesta HTTP equivalente.</returns>
    protected ActionResult ResponderSinContenido(Resultado resultado)
    {
        ArgumentNullException.ThrowIfNull(resultado);
        return resultado.EsExito ? NoContent() : ProblemaDesde(resultado.Error!);
    }

    /// <summary>
    /// Devuelve 201 con la cabecera <c>Location</c>, o el problema correspondiente al error.
    /// </summary>
    /// <typeparam name="T">Tipo del recurso creado.</typeparam>
    /// <param name="resultado">Resultado del caso de uso.</param>
    /// <param name="nombreRuta">Nombre de la ruta que permite consultar el recurso creado.</param>
    /// <param name="valoresRuta">Valores de ruta necesarios para construir la URL.</param>
    /// <returns>Respuesta HTTP equivalente.</returns>
    protected ActionResult<T> ResponderCreado<T>(Resultado<T> resultado, string nombreRuta, object valoresRuta)
    {
        ArgumentNullException.ThrowIfNull(resultado);

        return resultado.EsExito
            ? CreatedAtRoute(nombreRuta, valoresRuta, resultado.Valor)
            : ProblemaDesde(resultado.Error!);
    }

    /// <summary>
    /// Construye una respuesta <c>ProblemDetails</c> a partir de un error de aplicación.
    /// </summary>
    /// <param name="error">Error devuelto por el caso de uso.</param>
    /// <returns>Respuesta con el código HTTP correspondiente.</returns>
    protected ObjectResult ProblemaDesde(ErrorApp error)
    {
        ArgumentNullException.ThrowIfNull(error);

        int codigoHttp = MapearCodigoHttp(error.Tipo);

        var problema = new ProblemDetails
        {
            Title = TituloPara(error.Tipo),
            Status = codigoHttp,
            Detail = error.Mensaje,
            Instance = HttpContext.Request.Path,
        };

        // El código de error permite que los clientes reaccionen sin depender del texto del mensaje,
        // y el identificador de correlación enlaza la respuesta con la entrada correspondiente del registro.
        problema.Extensions["codigoError"] = error.Codigo;
        problema.Extensions["identificadorCorrelacion"] = HttpContext.TraceIdentifier;

        if (error.Campo is not null)
        {
            problema.Extensions["campo"] = error.Campo;
        }

        return StatusCode(codigoHttp, problema);
    }

    private static int MapearCodigoHttp(TipoError tipo) => tipo switch
    {
        TipoError.NoEncontrado => StatusCodes.Status404NotFound,
        TipoError.Conflicto => StatusCodes.Status409Conflict,
        TipoError.Concurrencia => StatusCodes.Status409Conflict,
        TipoError.Validacion => StatusCodes.Status422UnprocessableEntity,
        TipoError.ReglaNegocio => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status400BadRequest,
    };

    private static string TituloPara(TipoError tipo) => tipo switch
    {
        TipoError.NoEncontrado => "Recurso no encontrado",
        TipoError.Conflicto => "Conflicto con el estado actual del recurso",
        TipoError.Concurrencia => "Conflicto de concurrencia",
        TipoError.Validacion => "Datos de entrada inválidos",
        TipoError.ReglaNegocio => "Regla de negocio no cumplida",
        _ => "Solicitud incorrecta",
    };
}
