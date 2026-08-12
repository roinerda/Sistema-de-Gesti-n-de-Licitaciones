using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;

namespace Licitaciones.Application.Servicios;

/// <summary>
/// Casos de uso del módulo de proveedores.
/// </summary>
public interface IServicioProveedores
{
    /// <summary>Lista proveedores con paginación, filtro y ordenamiento.</summary>
    /// <param name="parametros">Parámetros de la consulta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Página de proveedores.</returns>
    Task<PaginaResultado<ProveedorDto>> ListarAsync(ParametrosConsultaProveedores parametros, CancellationToken cancelacion = default);

    /// <summary>Obtiene un proveedor por su identificador.</summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El proveedor o un error de recurso inexistente.</returns>
    Task<Resultado<ProveedorDto>> ObtenerAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Crea un proveedor.</summary>
    /// <param name="datos">Datos del proveedor.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El proveedor creado o el error correspondiente.</returns>
    Task<Resultado<ProveedorDto>> CrearAsync(GuardarProveedorDto datos, CancellationToken cancelacion = default);

    /// <summary>Actualiza el nombre de un proveedor.</summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="datos">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El proveedor actualizado o el error correspondiente.</returns>
    Task<Resultado<ProveedorDto>> ActualizarAsync(Guid id, GuardarProveedorDto datos, CancellationToken cancelacion = default);

    /// <summary>Aplica borrado lógico a un proveedor.</summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Resultado de la operación.</returns>
    Task<Resultado> EliminarAsync(Guid id, CancellationToken cancelacion = default);
}

/// <summary>
/// Casos de uso del módulo de licitaciones.
/// </summary>
public interface IServicioLicitaciones
{
    /// <summary>Lista licitaciones con paginación, filtro y ordenamiento.</summary>
    /// <param name="parametros">Parámetros de la consulta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Página de licitaciones.</returns>
    Task<PaginaResultado<LicitacionDto>> ListarAsync(ParametrosConsultaLicitaciones parametros, CancellationToken cancelacion = default);

    /// <summary>Obtiene una licitación con su evaluación de ofertas y transiciones disponibles.</summary>
    /// <param name="id">Identificador de la licitación.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El detalle de la licitación o un error de recurso inexistente.</returns>
    Task<Resultado<LicitacionDetalleDto>> ObtenerAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Crea una licitación en estado borrador.</summary>
    /// <param name="datos">Datos de la licitación.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La licitación creada o el error correspondiente.</returns>
    Task<Resultado<LicitacionDto>> CrearAsync(GuardarLicitacionDto datos, CancellationToken cancelacion = default);

    /// <summary>Actualiza los datos de una licitación.</summary>
    /// <param name="id">Identificador de la licitación.</param>
    /// <param name="datos">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La licitación actualizada o el error correspondiente.</returns>
    Task<Resultado<LicitacionDto>> ActualizarAsync(Guid id, GuardarLicitacionDto datos, CancellationToken cancelacion = default);

    /// <summary>Aplica una transición de estado.</summary>
    /// <param name="id">Identificador de la licitación.</param>
    /// <param name="datos">Estado destino.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La licitación actualizada o el error correspondiente.</returns>
    Task<Resultado<LicitacionDto>> CambiarEstadoAsync(Guid id, CambiarEstadoLicitacionDto datos, CancellationToken cancelacion = default);

    /// <summary>Aplica borrado lógico a una licitación.</summary>
    /// <param name="id">Identificador de la licitación.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Resultado de la operación.</returns>
    Task<Resultado> EliminarAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Obtiene la mejor oferta, su clasificación y el aprobador correspondiente.</summary>
    /// <param name="id">Identificador de la licitación.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La evaluación de ofertas o un error de recurso inexistente.</returns>
    Task<Resultado<MejorOfertaDto>> ObtenerMejorOfertaAsync(Guid id, CancellationToken cancelacion = default);
}

/// <summary>
/// Casos de uso del módulo de ofertas.
/// </summary>
public interface IServicioOfertas
{
    /// <summary>Lista ofertas con paginación, filtro por licitación o proveedor y ordenamiento.</summary>
    /// <param name="parametros">Parámetros de la consulta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Página de ofertas.</returns>
    Task<PaginaResultado<OfertaDto>> ListarAsync(ParametrosConsultaOfertas parametros, CancellationToken cancelacion = default);

    /// <summary>Obtiene una oferta por su identificador.</summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La oferta o un error de recurso inexistente.</returns>
    Task<Resultado<OfertaDto>> ObtenerAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Registra una oferta.</summary>
    /// <param name="datos">Datos de la oferta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La oferta creada o el error correspondiente.</returns>
    Task<Resultado<OfertaDto>> CrearAsync(GuardarOfertaDto datos, CancellationToken cancelacion = default);

    /// <summary>Modifica el monto de una oferta existente.</summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="datos">Nuevo monto.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La oferta actualizada o el error correspondiente.</returns>
    Task<Resultado<OfertaDto>> ActualizarAsync(Guid id, ActualizarOfertaDto datos, CancellationToken cancelacion = default);

    /// <summary>Elimina una oferta vigente.</summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Resultado de la operación.</returns>
    Task<Resultado> EliminarAsync(Guid id, CancellationToken cancelacion = default);
}

/// <summary>
/// Casos de uso del módulo de niveles de aprobación.
/// </summary>
public interface IServicioNivelesAprobacion
{
    /// <summary>Lista niveles con paginación y ordenamiento.</summary>
    /// <param name="parametros">Parámetros de la consulta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Página de niveles.</returns>
    Task<PaginaResultado<NivelAprobacionDto>> ListarAsync(ParametrosConsulta parametros, CancellationToken cancelacion = default);

    /// <summary>Obtiene un nivel por su identificador.</summary>
    /// <param name="id">Identificador del nivel.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El nivel o un error de recurso inexistente.</returns>
    Task<Resultado<NivelAprobacionDto>> ObtenerAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Crea un nivel de aprobación validando que el rango no se traslape.</summary>
    /// <param name="datos">Datos del nivel.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El nivel creado o el error correspondiente.</returns>
    Task<Resultado<NivelAprobacionDto>> CrearAsync(GuardarNivelAprobacionDto datos, CancellationToken cancelacion = default);

    /// <summary>Actualiza un nivel de aprobación validando que el rango no se traslape.</summary>
    /// <param name="id">Identificador del nivel.</param>
    /// <param name="datos">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El nivel actualizado o el error correspondiente.</returns>
    Task<Resultado<NivelAprobacionDto>> ActualizarAsync(Guid id, GuardarNivelAprobacionDto datos, CancellationToken cancelacion = default);

    /// <summary>Elimina un nivel de aprobación.</summary>
    /// <param name="id">Identificador del nivel.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Resultado de la operación.</returns>
    Task<Resultado> EliminarAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Determina el aprobador correspondiente a un monto.</summary>
    /// <param name="montoCrc">Monto en colones.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Nombre del aprobador o <see langword="null"/> si ningún rango cubre el monto.</returns>
    Task<string?> ObtenerAprobadorAsync(decimal montoCrc, CancellationToken cancelacion = default);
}

/// <summary>
/// Casos de uso del módulo de tipos de cambio.
/// </summary>
public interface IServicioTiposCambio
{
    /// <summary>Lista tipos de cambio con paginación y ordenamiento.</summary>
    /// <param name="parametros">Parámetros de la consulta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Página de tipos de cambio.</returns>
    Task<PaginaResultado<TipoCambioDto>> ListarAsync(ParametrosConsulta parametros, CancellationToken cancelacion = default);

    /// <summary>Obtiene un tipo de cambio por su identificador.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El tipo de cambio o un error de recurso inexistente.</returns>
    Task<Resultado<TipoCambioDto>> ObtenerAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Obtiene el tipo de cambio activo.</summary>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El tipo de cambio activo o <see langword="null"/> si no hay ninguno configurado.</returns>
    Task<TipoCambioDto?> ObtenerActivoAsync(CancellationToken cancelacion = default);

    /// <summary>Crea un tipo de cambio.</summary>
    /// <param name="datos">Datos del tipo de cambio.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El tipo de cambio creado o el error correspondiente.</returns>
    Task<Resultado<TipoCambioDto>> CrearAsync(GuardarTipoCambioDto datos, CancellationToken cancelacion = default);

    /// <summary>Actualiza un tipo de cambio.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="datos">Datos nuevos.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El tipo de cambio actualizado o el error correspondiente.</returns>
    Task<Resultado<TipoCambioDto>> ActualizarAsync(Guid id, GuardarTipoCambioDto datos, CancellationToken cancelacion = default);

    /// <summary>Marca un tipo de cambio como activo y desactiva los demás.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El tipo de cambio activado o el error correspondiente.</returns>
    Task<Resultado<TipoCambioDto>> ActivarAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Elimina un tipo de cambio que no esté activo.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Resultado de la operación.</returns>
    Task<Resultado> EliminarAsync(Guid id, CancellationToken cancelacion = default);
}

/// <summary>
/// Conversión referencial de montos a dólares.
/// </summary>
public interface IServicioConversionMoneda
{
    /// <summary>Convierte un monto en colones a dólares usando el tipo de cambio activo.</summary>
    /// <param name="montoCrc">Monto en colones.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El monto convertido o un error si no hay tipo de cambio activo.</returns>
    Task<Resultado<MontoConvertidoDto>> ConvertirAsync(decimal montoCrc, CancellationToken cancelacion = default);
}
