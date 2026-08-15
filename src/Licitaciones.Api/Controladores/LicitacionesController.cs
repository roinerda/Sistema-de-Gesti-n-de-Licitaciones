using Asp.Versioning;
using Licitaciones.Api.Comun;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Controladores;

/// <summary>
/// Administración de licitaciones, sus ofertas y su mejor oferta.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/licitaciones")]
public sealed class LicitacionesController : ControladorApiBase
{
    /// <summary>Nombre de la ruta que consulta una licitación por su identificador.</summary>
    public const string RutaObtener = "LicitacionesObtener";

    private readonly IServicioLicitaciones _servicio;
    private readonly IServicioOfertas _servicioOfertas;

    /// <summary>
    /// Crea el controlador.
    /// </summary>
    /// <param name="servicio">Casos de uso de licitaciones.</param>
    /// <param name="servicioOfertas">Casos de uso de ofertas.</param>
    public LicitacionesController(IServicioLicitaciones servicio, IServicioOfertas servicioOfertas)
    {
        _servicio = servicio;
        _servicioOfertas = servicioOfertas;
    }

    /// <summary>
    /// Lista licitaciones con paginación, filtrado por estado y ordenamiento.
    /// </summary>
    /// <param name="parametros">Parámetros de la consulta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Página de licitaciones.</returns>
    /// <response code="200">Listado obtenido correctamente.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginaResultado<LicitacionDto>>> Listar(
        [FromQuery] ParametrosConsultaLicitaciones parametros,
        CancellationToken cancelacion) =>
        Ok(await _servicio.ListarAsync(parametros, cancelacion));

    /// <summary>
    /// Consulta una licitación con su evaluación de ofertas y las transiciones disponibles.
    /// </summary>
    /// <param name="id">Identificador de la licitación.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Detalle de la licitación.</returns>
    /// <response code="200">Licitación encontrada.</response>
    /// <response code="404">No existe una licitación con ese identificador.</response>
    [HttpGet("{id:guid}", Name = RutaObtener)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<LicitacionDetalleDto>> Obtener(Guid id, CancellationToken cancelacion) =>
        Responder(await _servicio.ObtenerAsync(id, cancelacion));

    /// <summary>
    /// Crea una licitación en estado borrador.
    /// </summary>
    /// <param name="datos">Datos de la licitación.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La licitación creada.</returns>
    /// <response code="201">Licitación creada.</response>
    /// <response code="409">Ya existe una licitación con el mismo código.</response>
    /// <response code="422">Los datos incumplen una regla de negocio.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<LicitacionDto>> Crear(
        [FromBody] GuardarLicitacionDto datos,
        CancellationToken cancelacion)
    {
        Resultado<LicitacionDto> resultado = await _servicio.CrearAsync(datos, cancelacion);

        return ResponderCreado(
            resultado,
            RutaObtener,
            new { id = resultado.Valor?.Id, version = "1.0" });
    }

    /// <summary>
    /// Actualiza los datos de una licitación.
    /// </summary>
    /// <param name="id">Identificador de la licitación.</param>
    /// <param name="datos">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La licitación actualizada.</returns>
    /// <response code="200">Licitación actualizada.</response>
    /// <response code="404">No existe una licitación con ese identificador.</response>
    /// <response code="409">El código ya está en uso o el registro cambió mientras se editaba.</response>
    /// <response code="422">Los datos incumplen una regla de negocio.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<LicitacionDto>> Actualizar(
        Guid id,
        [FromBody] GuardarLicitacionDto datos,
        CancellationToken cancelacion) =>
        Responder(await _servicio.ActualizarAsync(id, datos, cancelacion));

    /// <summary>
    /// Aplica una transición de estado.
    /// </summary>
    /// <param name="id">Identificador de la licitación.</param>
    /// <param name="datos">Estado destino.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La licitación actualizada.</returns>
    /// <response code="200">Transición aplicada.</response>
    /// <response code="404">No existe una licitación con ese identificador.</response>
    /// <response code="422">La transición no está permitida o faltan condiciones para publicar.</response>
    [HttpPatch("{id:guid}/estado")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<LicitacionDto>> CambiarEstado(
        Guid id,
        [FromBody] CambiarEstadoLicitacionDto datos,
        CancellationToken cancelacion) =>
        Responder(await _servicio.CambiarEstadoAsync(id, datos, cancelacion));

    /// <summary>
    /// Aplica borrado lógico a una licitación y conserva sus ofertas como evidencia.
    /// </summary>
    /// <param name="id">Identificador de la licitación.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Sin contenido.</returns>
    /// <response code="204">Licitación eliminada.</response>
    /// <response code="404">No existe una licitación con ese identificador.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<ActionResult> Eliminar(Guid id, CancellationToken cancelacion) =>
        ResponderSinContenido(await _servicio.EliminarAsync(id, cancelacion));

    /// <summary>
    /// Lista las ofertas de una licitación.
    /// </summary>
    /// <param name="id">Identificador de la licitación.</param>
    /// <param name="parametros">Parámetros de paginación y ordenamiento.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Página de ofertas.</returns>
    /// <response code="200">Listado obtenido correctamente.</response>
    /// <response code="404">No existe una licitación con ese identificador.</response>
    [HttpGet("{id:guid}/ofertas")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<PaginaResultado<OfertaDto>>> ListarOfertas(
        Guid id,
        [FromQuery] ParametrosConsulta parametros,
        CancellationToken cancelacion)
    {
        Resultado<LicitacionDetalleDto> licitacion = await _servicio.ObtenerAsync(id, cancelacion);

        if (!licitacion.EsExito)
        {
            return ProblemaDesde(licitacion.Error!);
        }

        var consulta = new ParametrosConsultaOfertas
        {
            LicitacionId = id,
            Pagina = parametros.Pagina,
            TamanoPagina = parametros.TamanoPagina,
            OrdenarPor = parametros.OrdenarPor,
            Descendente = parametros.Descendente,
        };

        return Ok(await _servicioOfertas.ListarAsync(consulta, cancelacion));
    }

    /// <summary>
    /// Registra una oferta para la licitación indicada.
    /// </summary>
    /// <param name="id">Identificador de la licitación.</param>
    /// <param name="datos">Proveedor y monto ofertado.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La oferta creada.</returns>
    /// <response code="201">Oferta registrada.</response>
    /// <response code="404">No existe la licitación o el proveedor indicado.</response>
    /// <response code="409">El proveedor ya tiene una oferta en esta licitación.</response>
    /// <response code="422">La oferta supera el presupuesto, está vencida o la licitación no está publicada.</response>
    [HttpPost("{id:guid}/ofertas")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<OfertaDto>> CrearOferta(
        Guid id,
        [FromBody] CrearOfertaDto datos,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(datos);

        var completo = new GuardarOfertaDto
        {
            LicitacionId = id,
            ProveedorId = datos.ProveedorId,
            MontoOfertadoCrc = datos.MontoOfertadoCrc,
        };

        Resultado<OfertaDto> resultado = await _servicioOfertas.CrearAsync(completo, cancelacion);

        return ResponderCreado(
            resultado,
            OfertasController.RutaObtener,
            new { id = resultado.Valor?.Id, version = "1.0" });
    }

    /// <summary>
    /// Consulta la mejor oferta de la licitación, su ahorro, su clasificación y el aprobador.
    /// </summary>
    /// <param name="id">Identificador de la licitación.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Resultado de la evaluación de ofertas.</returns>
    /// <response code="200">Evaluación obtenida correctamente.</response>
    /// <response code="404">No existe una licitación con ese identificador.</response>
    [HttpGet("{id:guid}/mejor-oferta")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<MejorOfertaDto>> ObtenerMejorOferta(Guid id, CancellationToken cancelacion) =>
        Responder(await _servicio.ObtenerMejorOfertaAsync(id, cancelacion));
}
