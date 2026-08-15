using Asp.Versioning;
using Licitaciones.Api.Comun;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Controladores;

/// <summary>
/// Administración de proveedores.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/proveedores")]
public sealed class ProveedoresController : ControladorApiBase
{
    /// <summary>Nombre de la ruta que consulta un proveedor por su identificador.</summary>
    public const string RutaObtener = "ProveedoresObtener";

    private readonly IServicioProveedores _servicio;
    private readonly IServicioOfertas _servicioOfertas;

    /// <summary>
    /// Crea el controlador.
    /// </summary>
    /// <param name="servicio">Casos de uso de proveedores.</param>
    /// <param name="servicioOfertas">Casos de uso de ofertas, para consultar las de un proveedor.</param>
    public ProveedoresController(IServicioProveedores servicio, IServicioOfertas servicioOfertas)
    {
        _servicio = servicio;
        _servicioOfertas = servicioOfertas;
    }

    /// <summary>
    /// Lista proveedores con paginación, búsqueda y ordenamiento.
    /// </summary>
    /// <param name="parametros">Parámetros de paginación, búsqueda y ordenamiento.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Página de proveedores.</returns>
    /// <response code="200">Listado obtenido correctamente.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginaResultado<ProveedorDto>>> Listar(
        [FromQuery] ParametrosConsultaProveedores parametros,
        CancellationToken cancelacion) =>
        Ok(await _servicio.ListarAsync(parametros, cancelacion));

    /// <summary>
    /// Consulta un proveedor por su identificador.
    /// </summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El proveedor solicitado.</returns>
    /// <response code="200">Proveedor encontrado.</response>
    /// <response code="404">No existe un proveedor con ese identificador.</response>
    [HttpGet("{id:guid}", Name = RutaObtener)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<ProveedorDto>> Obtener(Guid id, CancellationToken cancelacion) =>
        Responder(await _servicio.ObtenerAsync(id, cancelacion));

    /// <summary>
    /// Consulta las ofertas registradas por un proveedor.
    /// </summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="parametros">Parámetros de paginación y ordenamiento.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Página de ofertas del proveedor.</returns>
    /// <response code="200">Listado obtenido correctamente.</response>
    /// <response code="404">No existe un proveedor con ese identificador.</response>
    [HttpGet("{id:guid}/ofertas")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<PaginaResultado<OfertaDto>>> ListarOfertas(
        Guid id,
        [FromQuery] ParametrosConsulta parametros,
        CancellationToken cancelacion)
    {
        Resultado<ProveedorDto> proveedor = await _servicio.ObtenerAsync(id, cancelacion);

        if (!proveedor.EsExito)
        {
            return ProblemaDesde(proveedor.Error!);
        }

        var consulta = new ParametrosConsultaOfertas
        {
            ProveedorId = id,
            Pagina = parametros.Pagina,
            TamanoPagina = parametros.TamanoPagina,
            OrdenarPor = parametros.OrdenarPor,
            Descendente = parametros.Descendente,
        };

        return Ok(await _servicioOfertas.ListarAsync(consulta, cancelacion));
    }

    /// <summary>
    /// Registra un proveedor.
    /// </summary>
    /// <param name="datos">Datos del proveedor.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El proveedor creado.</returns>
    /// <response code="201">Proveedor creado.</response>
    /// <response code="409">Ya existe un proveedor con el mismo nombre normalizado.</response>
    /// <response code="422">El nombre incumple una regla de negocio.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<ProveedorDto>> Crear(
        [FromBody] GuardarProveedorDto datos,
        CancellationToken cancelacion)
    {
        Resultado<ProveedorDto> resultado = await _servicio.CrearAsync(datos, cancelacion);

        return ResponderCreado(
            resultado,
            RutaObtener,
            new { id = resultado.Valor?.Id, version = "1.0" });
    }

    /// <summary>
    /// Actualiza el nombre de un proveedor.
    /// </summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="datos">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El proveedor actualizado.</returns>
    /// <response code="200">Proveedor actualizado.</response>
    /// <response code="404">No existe un proveedor con ese identificador.</response>
    /// <response code="409">El nombre ya está en uso o el registro cambió mientras se editaba.</response>
    /// <response code="422">El nombre incumple una regla de negocio.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<ProveedorDto>> Actualizar(
        Guid id,
        [FromBody] GuardarProveedorDto datos,
        CancellationToken cancelacion) =>
        Responder(await _servicio.ActualizarAsync(id, datos, cancelacion));

    /// <summary>
    /// Aplica borrado lógico a un proveedor y conserva sus ofertas como evidencia.
    /// </summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Sin contenido.</returns>
    /// <response code="204">Proveedor eliminado.</response>
    /// <response code="404">No existe un proveedor con ese identificador.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<ActionResult> Eliminar(Guid id, CancellationToken cancelacion) =>
        ResponderSinContenido(await _servicio.EliminarAsync(id, cancelacion));
}
