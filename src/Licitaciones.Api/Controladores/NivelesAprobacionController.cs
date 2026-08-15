using Asp.Versioning;
using Licitaciones.Api.Comun;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Controladores;

/// <summary>
/// Administración de los niveles de aprobación por rango de monto.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/niveles-aprobacion")]
public sealed class NivelesAprobacionController : ControladorApiBase
{
    /// <summary>Nombre de la ruta que consulta un nivel por su identificador.</summary>
    public const string RutaObtener = "NivelesAprobacionObtener";

    private readonly IServicioNivelesAprobacion _servicio;

    /// <summary>
    /// Crea el controlador.
    /// </summary>
    /// <param name="servicio">Casos de uso de niveles de aprobación.</param>
    public NivelesAprobacionController(IServicioNivelesAprobacion servicio) => _servicio = servicio;

    /// <summary>
    /// Lista los niveles de aprobación configurados.
    /// </summary>
    /// <param name="parametros">Parámetros de la consulta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Página de niveles.</returns>
    /// <response code="200">Listado obtenido correctamente.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginaResultado<NivelAprobacionDto>>> Listar(
        [FromQuery] ParametrosConsulta parametros,
        CancellationToken cancelacion) =>
        Ok(await _servicio.ListarAsync(parametros, cancelacion));

    /// <summary>
    /// Consulta un nivel de aprobación por su identificador.
    /// </summary>
    /// <param name="id">Identificador del nivel.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El nivel solicitado.</returns>
    /// <response code="200">Nivel encontrado.</response>
    /// <response code="404">No existe un nivel con ese identificador.</response>
    [HttpGet("{id:guid}", Name = RutaObtener)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<NivelAprobacionDto>> Obtener(Guid id, CancellationToken cancelacion) =>
        Responder(await _servicio.ObtenerAsync(id, cancelacion));

    /// <summary>
    /// Consulta el aprobador que corresponde a un monto en colones.
    /// </summary>
    /// <param name="montoCrc">Monto a clasificar.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Nombre del aprobador, o 404 si ningún rango cubre el monto.</returns>
    /// <response code="200">Aprobador determinado.</response>
    /// <response code="404">Ningún rango configurado cubre ese monto.</response>
    [HttpGet("aprobador")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<string>> ObtenerAprobador(
        [FromQuery] decimal montoCrc,
        CancellationToken cancelacion)
    {
        string? aprobador = await _servicio.ObtenerAprobadorAsync(montoCrc, cancelacion);

        return aprobador is null
            ? ProblemaDesde(ErrorApp.NoEncontrado("Ningún nivel de aprobación configurado cubre ese monto."))
            : Ok(aprobador);
    }

    /// <summary>
    /// Crea un nivel de aprobación.
    /// </summary>
    /// <param name="datos">Datos del nivel.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El nivel creado.</returns>
    /// <response code="201">Nivel creado.</response>
    /// <response code="422">El rango se traslapa, es incoherente o duplica el rango abierto.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<NivelAprobacionDto>> Crear(
        [FromBody] GuardarNivelAprobacionDto datos,
        CancellationToken cancelacion)
    {
        Resultado<NivelAprobacionDto> resultado = await _servicio.CrearAsync(datos, cancelacion);

        return ResponderCreado(
            resultado,
            RutaObtener,
            new { id = resultado.Valor?.Id, version = "1.0" });
    }

    /// <summary>
    /// Actualiza un nivel de aprobación.
    /// </summary>
    /// <param name="id">Identificador del nivel.</param>
    /// <param name="datos">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El nivel actualizado.</returns>
    /// <response code="200">Nivel actualizado.</response>
    /// <response code="404">No existe un nivel con ese identificador.</response>
    /// <response code="422">El rango se traslapa, es incoherente o duplica el rango abierto.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<NivelAprobacionDto>> Actualizar(
        Guid id,
        [FromBody] GuardarNivelAprobacionDto datos,
        CancellationToken cancelacion) =>
        Responder(await _servicio.ActualizarAsync(id, datos, cancelacion));

    /// <summary>
    /// Elimina un nivel de aprobación.
    /// </summary>
    /// <param name="id">Identificador del nivel.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Sin contenido.</returns>
    /// <response code="204">Nivel eliminado.</response>
    /// <response code="404">No existe un nivel con ese identificador.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<ActionResult> Eliminar(Guid id, CancellationToken cancelacion) =>
        ResponderSinContenido(await _servicio.EliminarAsync(id, cancelacion));
}
