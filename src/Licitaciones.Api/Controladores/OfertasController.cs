using Asp.Versioning;
using Licitaciones.Api.Comun;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Controladores;

/// <summary>
/// Administración de ofertas.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ofertas")]
public sealed class OfertasController : ControladorApiBase
{
    /// <summary>Nombre de la ruta que consulta una oferta por su identificador.</summary>
    public const string RutaObtener = "OfertasObtener";

    private readonly IServicioOfertas _servicio;

    /// <summary>
    /// Crea el controlador.
    /// </summary>
    /// <param name="servicio">Casos de uso de ofertas.</param>
    public OfertasController(IServicioOfertas servicio) => _servicio = servicio;

    /// <summary>
    /// Lista ofertas con paginación y filtrado por licitación o proveedor.
    /// </summary>
    /// <param name="parametros">Parámetros de la consulta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Página de ofertas.</returns>
    /// <response code="200">Listado obtenido correctamente.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginaResultado<OfertaDto>>> Listar(
        [FromQuery] ParametrosConsultaOfertas parametros,
        CancellationToken cancelacion) =>
        Ok(await _servicio.ListarAsync(parametros, cancelacion));

    /// <summary>
    /// Consulta una oferta por su identificador.
    /// </summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La oferta solicitada.</returns>
    /// <response code="200">Oferta encontrada.</response>
    /// <response code="404">No existe una oferta con ese identificador.</response>
    [HttpGet("{id:guid}", Name = RutaObtener)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<OfertaDto>> Obtener(Guid id, CancellationToken cancelacion) =>
        Responder(await _servicio.ObtenerAsync(id, cancelacion));

    /// <summary>
    /// Registra una oferta indicando licitación, proveedor y monto.
    /// </summary>
    /// <param name="datos">Datos de la oferta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La oferta creada.</returns>
    /// <response code="201">Oferta registrada.</response>
    /// <response code="404">No existe la licitación o el proveedor indicado.</response>
    /// <response code="409">El proveedor ya tiene una oferta en esta licitación.</response>
    /// <response code="422">La oferta supera el presupuesto, está vencida o la licitación no está publicada.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<OfertaDto>> Crear(
        [FromBody] GuardarOfertaDto datos,
        CancellationToken cancelacion)
    {
        Resultado<OfertaDto> resultado = await _servicio.CrearAsync(datos, cancelacion);

        return ResponderCreado(
            resultado,
            RutaObtener,
            new { id = resultado.Valor?.Id, version = "1.0" });
    }

    /// <summary>
    /// Modifica el monto de una oferta vigente.
    /// </summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="datos">Nuevo monto.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La oferta actualizada.</returns>
    /// <response code="200">Oferta actualizada.</response>
    /// <response code="404">No existe una oferta con ese identificador.</response>
    /// <response code="409">El registro cambió mientras se editaba.</response>
    /// <response code="422">La licitación ya no admite cambios o el monto es inválido.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<OfertaDto>> Actualizar(
        Guid id,
        [FromBody] ActualizarOfertaDto datos,
        CancellationToken cancelacion) =>
        Responder(await _servicio.ActualizarAsync(id, datos, cancelacion));

    /// <summary>
    /// Elimina una oferta de una licitación vigente.
    /// </summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Sin contenido.</returns>
    /// <response code="204">Oferta eliminada.</response>
    /// <response code="404">No existe una oferta con ese identificador.</response>
    /// <response code="422">La licitación está cerrada o vencida: la oferta se conserva como evidencia.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ProblemDetails))]
    public async Task<ActionResult> Eliminar(Guid id, CancellationToken cancelacion) =>
        ResponderSinContenido(await _servicio.EliminarAsync(id, cancelacion));
}
