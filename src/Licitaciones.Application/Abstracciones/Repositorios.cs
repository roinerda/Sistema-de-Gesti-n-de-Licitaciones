using Licitaciones.Application.Comun;
using Licitaciones.Domain.Entidades;

namespace Licitaciones.Application.Abstracciones;

/// <summary>
/// Puerto de acceso a proveedores.
/// </summary>
public interface IRepositorioProveedores
{
    /// <summary>Obtiene un proveedor por su identificador.</summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="incluirEliminados">Incluye proveedores dados de baja lógicamente.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El proveedor o <see langword="null"/> si no existe.</returns>
    Task<Proveedor?> ObtenerPorIdAsync(Guid id, bool incluirEliminados = false, CancellationToken cancelacion = default);

    /// <summary>Indica si ya existe otro proveedor con el mismo nombre normalizado.</summary>
    /// <param name="nombreNormalizado">Nombre normalizado a buscar.</param>
    /// <param name="idExcluido">Identificador a excluir, útil al editar.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns><see langword="true"/> si el nombre ya está en uso.</returns>
    Task<bool> ExisteNombreAsync(string nombreNormalizado, Guid? idExcluido = null, CancellationToken cancelacion = default);

    /// <summary>Lista proveedores con paginación, filtro y ordenamiento.</summary>
    /// <param name="parametros">Parámetros de la consulta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Página de proveedores.</returns>
    Task<PaginaResultado<Proveedor>> ListarAsync(ParametrosConsultaProveedores parametros, CancellationToken cancelacion = default);

    /// <summary>Cuenta las ofertas registradas por un proveedor.</summary>
    /// <param name="proveedorId">Identificador del proveedor.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Cantidad de ofertas relacionadas.</returns>
    Task<int> ContarOfertasAsync(Guid proveedorId, CancellationToken cancelacion = default);

    /// <summary>Cuenta las ofertas de varios proveedores en una sola consulta.</summary>
    /// <param name="proveedoresIds">Identificadores de los proveedores.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Diccionario de identificador de proveedor a cantidad de ofertas.</returns>
    Task<IReadOnlyDictionary<Guid, int>> ContarOfertasAsync(IReadOnlyCollection<Guid> proveedoresIds, CancellationToken cancelacion = default);

    /// <summary>Marca un proveedor nuevo para inserción.</summary>
    /// <param name="proveedor">Proveedor a insertar.</param>
    void Agregar(Proveedor proveedor);
}

/// <summary>
/// Puerto de acceso a licitaciones.
/// </summary>
public interface IRepositorioLicitaciones
{
    /// <summary>Obtiene una licitación por su identificador.</summary>
    /// <param name="id">Identificador de la licitación.</param>
    /// <param name="incluirEliminadas">Incluye licitaciones dadas de baja lógicamente.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La licitación o <see langword="null"/> si no existe.</returns>
    Task<Licitacion?> ObtenerPorIdAsync(Guid id, bool incluirEliminadas = false, CancellationToken cancelacion = default);

    /// <summary>Indica si ya existe otra licitación con el mismo código normalizado.</summary>
    /// <param name="codigoNormalizado">Código normalizado a buscar.</param>
    /// <param name="idExcluido">Identificador a excluir, útil al editar.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns><see langword="true"/> si el código ya está en uso.</returns>
    Task<bool> ExisteCodigoAsync(string codigoNormalizado, Guid? idExcluido = null, CancellationToken cancelacion = default);

    /// <summary>Lista licitaciones con paginación, filtro y ordenamiento.</summary>
    /// <param name="parametros">Parámetros de la consulta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Página de licitaciones.</returns>
    Task<PaginaResultado<Licitacion>> ListarAsync(ParametrosConsultaLicitaciones parametros, CancellationToken cancelacion = default);

    /// <summary>Obtiene el monto de la oferta más alta registrada para una licitación.</summary>
    /// <param name="licitacionId">Identificador de la licitación.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El monto mayor o <see langword="null"/> si no hay ofertas.</returns>
    Task<decimal?> ObtenerMontoOfertaMayorAsync(Guid licitacionId, CancellationToken cancelacion = default);

    /// <summary>Cuenta las ofertas registradas para una licitación.</summary>
    /// <param name="licitacionId">Identificador de la licitación.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Cantidad de ofertas relacionadas.</returns>
    Task<int> ContarOfertasAsync(Guid licitacionId, CancellationToken cancelacion = default);

    /// <summary>Cuenta las ofertas de varias licitaciones en una sola consulta.</summary>
    /// <param name="licitacionesIds">Identificadores de las licitaciones.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Diccionario de identificador de licitación a cantidad de ofertas.</returns>
    Task<IReadOnlyDictionary<Guid, int>> ContarOfertasAsync(IReadOnlyCollection<Guid> licitacionesIds, CancellationToken cancelacion = default);

    /// <summary>Marca una licitación nueva para inserción.</summary>
    /// <param name="licitacion">Licitación a insertar.</param>
    void Agregar(Licitacion licitacion);
}

/// <summary>
/// Puerto de acceso a ofertas.
/// </summary>
public interface IRepositorioOfertas
{
    /// <summary>Obtiene una oferta con su licitación y proveedor.</summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>La oferta o <see langword="null"/> si no existe.</returns>
    Task<Oferta?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Indica si el proveedor ya tiene una oferta en la licitación.</summary>
    /// <param name="licitacionId">Identificador de la licitación.</param>
    /// <param name="proveedorId">Identificador del proveedor.</param>
    /// <param name="idExcluido">Identificador de oferta a excluir, útil al editar.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns><see langword="true"/> si ya existe una oferta del proveedor.</returns>
    Task<bool> ExisteOfertaDeProveedorAsync(Guid licitacionId, Guid proveedorId, Guid? idExcluido = null, CancellationToken cancelacion = default);

    /// <summary>Lista ofertas con paginación, filtro y ordenamiento.</summary>
    /// <param name="parametros">Parámetros de la consulta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Página de ofertas con navegaciones cargadas.</returns>
    Task<PaginaResultado<Oferta>> ListarAsync(ParametrosConsultaOfertas parametros, CancellationToken cancelacion = default);

    /// <summary>Obtiene todas las ofertas de una licitación.</summary>
    /// <param name="licitacionId">Identificador de la licitación.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Ofertas ordenadas por monto y fecha de registro.</returns>
    Task<IReadOnlyList<Oferta>> ListarPorLicitacionAsync(Guid licitacionId, CancellationToken cancelacion = default);

    /// <summary>Marca una oferta nueva para inserción.</summary>
    /// <param name="oferta">Oferta a insertar.</param>
    void Agregar(Oferta oferta);

    /// <summary>Marca una oferta para eliminación física.</summary>
    /// <param name="oferta">Oferta a eliminar.</param>
    void Eliminar(Oferta oferta);
}

/// <summary>
/// Puerto de acceso a niveles de aprobación.
/// </summary>
public interface IRepositorioNivelesAprobacion
{
    /// <summary>Obtiene un nivel por su identificador.</summary>
    /// <param name="id">Identificador del nivel.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El nivel o <see langword="null"/> si no existe.</returns>
    Task<NivelAprobacion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Obtiene todos los niveles ordenados por monto mínimo.</summary>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Lista completa de niveles.</returns>
    Task<IReadOnlyList<NivelAprobacion>> ListarTodosAsync(CancellationToken cancelacion = default);

    /// <summary>Lista niveles con paginación y ordenamiento.</summary>
    /// <param name="parametros">Parámetros de la consulta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Página de niveles.</returns>
    Task<PaginaResultado<NivelAprobacion>> ListarAsync(ParametrosConsulta parametros, CancellationToken cancelacion = default);

    /// <summary>Marca un nivel nuevo para inserción.</summary>
    /// <param name="nivel">Nivel a insertar.</param>
    void Agregar(NivelAprobacion nivel);

    /// <summary>Marca un nivel para eliminación física.</summary>
    /// <param name="nivel">Nivel a eliminar.</param>
    void Eliminar(NivelAprobacion nivel);
}

/// <summary>
/// Puerto de acceso a tipos de cambio.
/// </summary>
public interface IRepositorioTiposCambio
{
    /// <summary>Obtiene un tipo de cambio por su identificador.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El tipo de cambio o <see langword="null"/> si no existe.</returns>
    Task<TipoCambio?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Obtiene el tipo de cambio activo.</summary>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El tipo de cambio activo o <see langword="null"/> si no hay ninguno.</returns>
    Task<TipoCambio?> ObtenerActivoAsync(CancellationToken cancelacion = default);

    /// <summary>Obtiene todos los tipos de cambio marcados como activos.</summary>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Lista de tipos de cambio activos.</returns>
    Task<IReadOnlyList<TipoCambio>> ListarActivosAsync(CancellationToken cancelacion = default);

    /// <summary>Lista tipos de cambio con paginación y ordenamiento.</summary>
    /// <param name="parametros">Parámetros de la consulta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Página de tipos de cambio.</returns>
    Task<PaginaResultado<TipoCambio>> ListarAsync(ParametrosConsulta parametros, CancellationToken cancelacion = default);

    /// <summary>Marca un tipo de cambio nuevo para inserción.</summary>
    /// <param name="tipoCambio">Tipo de cambio a insertar.</param>
    void Agregar(TipoCambio tipoCambio);

    /// <summary>Marca un tipo de cambio para eliminación física.</summary>
    /// <param name="tipoCambio">Tipo de cambio a eliminar.</param>
    void Eliminar(TipoCambio tipoCambio);
}
