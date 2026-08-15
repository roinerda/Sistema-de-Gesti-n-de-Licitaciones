using Asp.Versioning;
using Licitaciones.Api.Comun;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Controladores;

/// <summary>
/// Administración del tipo de cambio y conversión referencial de montos.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tipos-cambio")]
public sealed class TiposCambioController : ControladorApiBase
{
    /// <summary>Nombre de la ruta que consulta un tipo de cambio por su identificador.</summary>
    public const string RutaObtener = "TiposCambioObtener";

    private readonly IServicioTiposCambio _servicio;
    private readonly IServicioConversionMoneda _conversion;

    /// <summary>
    /// Crea el controlador.
    /// </summary>
    /// <param name="servicio">Casos de uso de tipos de cambio.</param>
    /// <param name="conversion">Servicio de conversión referencial.</param>
    public TiposCambioController(IServicioTiposCambio servicio, IServicioConversionMoneda conversion)
    {
        _servicio = servicio;
        _conversion = conversion;
    }

    /// <summary>
    /// Lista los tipos de cambio registrados.
    /// </summary>
    /// <param name="parametros">Parámetros de la consulta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Página de tipos de cambio.</returns>
    /// <response code="200">Listado obtenido correctamente.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginaResultado<TipoCambioDto>>> Listar(
        [FromQuery] ParametrosConsulta parametros,
        CancellationToken cancelacion) =>
        Ok(await _servicio.ListarAsync(parametros, cancelacion));

    /// <summary>
    /// Consulta el tipo de cambio activo.
    /// </summary>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El tipo de cambio vigente.</returns>
    /// <response code="200">Tipo de cambio activo encontrado.</response>
    /// <response code="404">No hay ningún tipo de cambio activo configurado.</response>
    [HttpGet("activo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<TipoCambioDto>> ObtenerActivo(CancellationToken cancelacion)
    {
        TipoCambioDto? activo = await _servicio.ObtenerActivoAsync(cancelacion);

        return activo is null
            ? ProblemaDesde(ErrorApp.NoEncontrado("No hay un tipo de cambio activo configurado."))
            : Ok(activo);
    }

    /// <summary>
    /// Convierte un monto en colones a dólares con el tipo de cambio activo.
    /// </summary>
    /// <param name="montoCrc">Monto en colones.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El monto en ambas monedas junto con la fecha del tipo de cambio aplicado.</returns>
    /// <response code="200">Conversión calculada.</response>
    /// <response code="409">No hay un tipo de cambio activo configurado.</response>
    [HttpGet("conversion")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<MontoConvertidoDto>> Convertir(
        [FromQuery] decimal montoCrc,
        CancellationToken cancelacion) =>
        Responder(await _conversion.ConvertirAsync(montoCrc, cancelacion));

    /// <summary>
    /// Consulta un tipo de cambio por su identificador.
    /// </summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El tipo de cambio solicitado.</returns>
    /// <response code="200">Tipo de cambio encontrado.</response>
    /// <response code="404">No existe un tipo de cambio con ese identificador.</response>
    [HttpGet("{id:guid}", Name = RutaObtener)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<TipoCambioDto>> Obtener(Guid id, CancellationToken cancelacion) =>
        Responder(await _servicio.ObtenerAsync(id, cancelacion));

    /// <summary>
    /// Registra un tipo de cambio.
    /// </summary>
    /// <param name="datos">Datos del tipo de cambio.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El tipo de cambio creado.</returns>
    /// <response code="201">Tipo de cambio creado.</response>
    /// <response code="422">El valor no es mayor que cero.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<TipoCambioDto>> Crear(
        [FromBody] GuardarTipoCambioDto datos,
        CancellationToken cancelacion)
    {
        Resultado<TipoCambioDto> resultado = await _servicio.CrearAsync(datos, cancelacion);

        return ResponderCreado(
            resultado,
            RutaObtener,
            new { id = resultado.Valor?.Id, version = "1.0" });
    }

    /// <summary>
    /// Actualiza un tipo de cambio.
    /// </summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="datos">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El tipo de cambio actualizado.</returns>
    /// <response code="200">Tipo de cambio actualizado.</response>
    /// <response code="404">No existe un tipo de cambio con ese identificador.</response>
    /// <response code="422">El valor no es mayor que cero.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<TipoCambioDto>> Actualizar(
        Guid id,
        [FromBody] GuardarTipoCambioDto datos,
        CancellationToken cancelacion) =>
        Responder(await _servicio.ActualizarAsync(id, datos, cancelacion));

    /// <summary>
    /// Marca un tipo de cambio como activo y desactiva los demás.
    /// </summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El tipo de cambio activado.</returns>
    /// <response code="200">Tipo de cambio activado.</response>
    /// <response code="404">No existe un tipo de cambio con ese identificador.</response>
    [HttpPatch("{id:guid}/activar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<TipoCambioDto>> Activar(Guid id, CancellationToken cancelacion) =>
        Responder(await _servicio.ActivarAsync(id, cancelacion));

    /// <summary>
    /// Elimina un tipo de cambio que no esté activo.
    /// </summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Sin contenido.</returns>
    /// <response code="204">Tipo de cambio eliminado.</response>
    /// <response code="404">No existe un tipo de cambio con ese identificador.</response>
    /// <response code="409">No se puede eliminar el tipo de cambio activo.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    public async Task<ActionResult> Eliminar(Guid id, CancellationToken cancelacion) =>
        ResponderSinContenido(await _servicio.EliminarAsync(id, cancelacion));
}
