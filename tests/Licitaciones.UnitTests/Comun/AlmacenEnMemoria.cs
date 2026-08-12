using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Entidades;

namespace Licitaciones.UnitTests.Comun;

/// <summary>
/// Almacén compartido por los repositorios en memoria.
/// </summary>
public sealed class AlmacenEnMemoria
{
    public List<Proveedor> Proveedores { get; } = [];

    public List<Licitacion> Licitaciones { get; } = [];

    public List<Oferta> Ofertas { get; } = [];

    public List<NivelAprobacion> NivelesAprobacion { get; } = [];

    public List<TipoCambio> TiposCambio { get; } = [];

    public int VecesGuardado { get; set; }

    public int TransaccionesEjecutadas { get; set; }

    public Dictionary<Guid, int> VersionesOriginales { get; } = [];
}

/// <summary>
/// Unidad de trabajo en memoria: cuenta confirmaciones y ejecuta la operación transaccional en línea.
/// </summary>
public sealed class UnidadDeTrabajoEnMemoria : IUnidadDeTrabajo
{
    private readonly AlmacenEnMemoria _almacen;

    public UnidadDeTrabajoEnMemoria(AlmacenEnMemoria almacen) => _almacen = almacen;

    public Task<int> GuardarCambiosAsync(CancellationToken cancelacion = default)
    {
        _almacen.VecesGuardado++;
        return Task.FromResult(1);
    }

    public void EstablecerVersionOriginal(EntidadBase entidad, int version) =>
        _almacen.VersionesOriginales[entidad.Id] = version;

    public Task<T> EjecutarEnTransaccionAsync<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken cancelacion = default)
    {
        _almacen.TransaccionesEjecutadas++;
        return operacion(cancelacion);
    }
}

/// <summary>Repositorio de proveedores en memoria.</summary>
public sealed class RepositorioProveedoresEnMemoria : IRepositorioProveedores
{
    private readonly AlmacenEnMemoria _almacen;

    public RepositorioProveedoresEnMemoria(AlmacenEnMemoria almacen) => _almacen = almacen;

    public Task<Proveedor?> ObtenerPorIdAsync(
        Guid id,
        bool incluirEliminados = false,
        CancellationToken cancelacion = default) =>
        Task.FromResult(_almacen.Proveedores
            .Find(p => p.Id == id && (incluirEliminados || !p.EstaEliminado)));

    public Task<bool> ExisteNombreAsync(
        string nombreNormalizado,
        Guid? idExcluido = null,
        CancellationToken cancelacion = default) =>
        Task.FromResult(_almacen.Proveedores.Exists(p =>
            p.NombreNormalizado == nombreNormalizado && !p.EstaEliminado && p.Id != idExcluido));

    public Task<PaginaResultado<Proveedor>> ListarAsync(
        ParametrosConsultaProveedores parametros,
        CancellationToken cancelacion = default)
    {
        IEnumerable<Proveedor> consulta = _almacen.Proveedores;

        if (!parametros.IncluirEliminados)
        {
            consulta = consulta.Where(p => !p.EstaEliminado);
        }

        if (!string.IsNullOrWhiteSpace(parametros.Buscar))
        {
            string patron = Domain.Normalizacion.NormalizadorTexto.NormalizarNombre(parametros.Buscar);
            consulta = consulta.Where(p => p.NombreNormalizado.Contains(patron, StringComparison.Ordinal));
        }

        var ordenada = consulta.OrderBy(p => p.NombreNormalizado, StringComparer.Ordinal).ToList();

        return Task.FromResult(new PaginaResultado<Proveedor>(
            ordenada.Skip(parametros.Omitir).Take(parametros.TamanoPagina).ToList(),
            parametros.Pagina,
            parametros.TamanoPagina,
            ordenada.Count));
    }

    public Task<int> ContarOfertasAsync(Guid proveedorId, CancellationToken cancelacion = default) =>
        Task.FromResult(_almacen.Ofertas.Count(o => o.ProveedorId == proveedorId));

    public Task<IReadOnlyDictionary<Guid, int>> ContarOfertasAsync(
        IReadOnlyCollection<Guid> proveedoresIds,
        CancellationToken cancelacion = default)
    {
        IReadOnlyDictionary<Guid, int> conteos = _almacen.Ofertas
            .Where(o => proveedoresIds.Contains(o.ProveedorId))
            .GroupBy(o => o.ProveedorId)
            .ToDictionary(g => g.Key, g => g.Count());

        return Task.FromResult(conteos);
    }

    public void Agregar(Proveedor proveedor) => _almacen.Proveedores.Add(proveedor);
}

/// <summary>Repositorio de licitaciones en memoria.</summary>
public sealed class RepositorioLicitacionesEnMemoria : IRepositorioLicitaciones
{
    private readonly AlmacenEnMemoria _almacen;

    public RepositorioLicitacionesEnMemoria(AlmacenEnMemoria almacen) => _almacen = almacen;

    public Task<Licitacion?> ObtenerPorIdAsync(
        Guid id,
        bool incluirEliminadas = false,
        CancellationToken cancelacion = default) =>
        Task.FromResult(_almacen.Licitaciones
            .Find(l => l.Id == id && (incluirEliminadas || !l.EstaEliminada)));

    public Task<bool> ExisteCodigoAsync(
        string codigoNormalizado,
        Guid? idExcluido = null,
        CancellationToken cancelacion = default) =>
        Task.FromResult(_almacen.Licitaciones.Exists(l =>
            l.CodigoNormalizado == codigoNormalizado && !l.EstaEliminada && l.Id != idExcluido));

    public Task<PaginaResultado<Licitacion>> ListarAsync(
        ParametrosConsultaLicitaciones parametros,
        CancellationToken cancelacion = default)
    {
        IEnumerable<Licitacion> consulta = _almacen.Licitaciones;

        if (!parametros.IncluirEliminadas)
        {
            consulta = consulta.Where(l => !l.EstaEliminada);
        }

        if (parametros.Estado is { } estado)
        {
            consulta = consulta.Where(l => l.Estado == estado);
        }

        var ordenada = consulta.OrderBy(l => l.FechaCierre).ToList();

        return Task.FromResult(new PaginaResultado<Licitacion>(
            ordenada.Skip(parametros.Omitir).Take(parametros.TamanoPagina).ToList(),
            parametros.Pagina,
            parametros.TamanoPagina,
            ordenada.Count));
    }

    public Task<decimal?> ObtenerMontoOfertaMayorAsync(Guid licitacionId, CancellationToken cancelacion = default)
    {
        var montos = _almacen.Ofertas.Where(o => o.LicitacionId == licitacionId).ToList();
        return Task.FromResult(montos.Count == 0 ? null : (decimal?)montos.Max(o => o.MontoOfertadoCrc));
    }

    public Task<int> ContarOfertasAsync(Guid licitacionId, CancellationToken cancelacion = default) =>
        Task.FromResult(_almacen.Ofertas.Count(o => o.LicitacionId == licitacionId));

    public Task<IReadOnlyDictionary<Guid, int>> ContarOfertasAsync(
        IReadOnlyCollection<Guid> licitacionesIds,
        CancellationToken cancelacion = default)
    {
        IReadOnlyDictionary<Guid, int> conteos = _almacen.Ofertas
            .Where(o => licitacionesIds.Contains(o.LicitacionId))
            .GroupBy(o => o.LicitacionId)
            .ToDictionary(g => g.Key, g => g.Count());

        return Task.FromResult(conteos);
    }

    public void Agregar(Licitacion licitacion) => _almacen.Licitaciones.Add(licitacion);
}

/// <summary>Repositorio de ofertas en memoria.</summary>
public sealed class RepositorioOfertasEnMemoria : IRepositorioOfertas
{
    private readonly AlmacenEnMemoria _almacen;

    public RepositorioOfertasEnMemoria(AlmacenEnMemoria almacen) => _almacen = almacen;

    public Task<Oferta?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        Task.FromResult(_almacen.Ofertas.Find(o => o.Id == id));

    public Task<bool> ExisteOfertaDeProveedorAsync(
        Guid licitacionId,
        Guid proveedorId,
        Guid? idExcluido = null,
        CancellationToken cancelacion = default) =>
        Task.FromResult(_almacen.Ofertas.Exists(o =>
            o.LicitacionId == licitacionId && o.ProveedorId == proveedorId && o.Id != idExcluido));

    public Task<PaginaResultado<Oferta>> ListarAsync(
        ParametrosConsultaOfertas parametros,
        CancellationToken cancelacion = default)
    {
        IEnumerable<Oferta> consulta = _almacen.Ofertas;

        if (parametros.LicitacionId is { } licitacionId)
        {
            consulta = consulta.Where(o => o.LicitacionId == licitacionId);
        }

        if (parametros.ProveedorId is { } proveedorId)
        {
            consulta = consulta.Where(o => o.ProveedorId == proveedorId);
        }

        var ordenada = consulta.OrderBy(o => o.MontoOfertadoCrc).ThenBy(o => o.FechaRegistro).ToList();

        return Task.FromResult(new PaginaResultado<Oferta>(
            ordenada.Skip(parametros.Omitir).Take(parametros.TamanoPagina).ToList(),
            parametros.Pagina,
            parametros.TamanoPagina,
            ordenada.Count));
    }

    public Task<IReadOnlyList<Oferta>> ListarPorLicitacionAsync(
        Guid licitacionId,
        CancellationToken cancelacion = default)
    {
        IReadOnlyList<Oferta> ofertas = _almacen.Ofertas
            .Where(o => o.LicitacionId == licitacionId)
            .OrderBy(o => o.MontoOfertadoCrc)
            .ThenBy(o => o.FechaRegistro)
            .ToList();

        return Task.FromResult(ofertas);
    }

    public void Agregar(Oferta oferta) => _almacen.Ofertas.Add(oferta);

    public void Eliminar(Oferta oferta) => _almacen.Ofertas.Remove(oferta);
}

/// <summary>Repositorio de niveles de aprobación en memoria.</summary>
public sealed class RepositorioNivelesAprobacionEnMemoria : IRepositorioNivelesAprobacion
{
    private readonly AlmacenEnMemoria _almacen;

    public RepositorioNivelesAprobacionEnMemoria(AlmacenEnMemoria almacen) => _almacen = almacen;

    public Task<NivelAprobacion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        Task.FromResult(_almacen.NivelesAprobacion.Find(n => n.Id == id));

    public Task<IReadOnlyList<NivelAprobacion>> ListarTodosAsync(CancellationToken cancelacion = default)
    {
        IReadOnlyList<NivelAprobacion> niveles = _almacen.NivelesAprobacion
            .OrderBy(n => n.MontoMinimoCrc)
            .ToList();

        return Task.FromResult(niveles);
    }

    public Task<PaginaResultado<NivelAprobacion>> ListarAsync(
        ParametrosConsulta parametros,
        CancellationToken cancelacion = default)
    {
        var ordenada = _almacen.NivelesAprobacion.OrderBy(n => n.MontoMinimoCrc).ToList();

        return Task.FromResult(new PaginaResultado<NivelAprobacion>(
            ordenada.Skip(parametros.Omitir).Take(parametros.TamanoPagina).ToList(),
            parametros.Pagina,
            parametros.TamanoPagina,
            ordenada.Count));
    }

    public void Agregar(NivelAprobacion nivel) => _almacen.NivelesAprobacion.Add(nivel);

    public void Eliminar(NivelAprobacion nivel) => _almacen.NivelesAprobacion.Remove(nivel);
}

/// <summary>Repositorio de tipos de cambio en memoria.</summary>
public sealed class RepositorioTiposCambioEnMemoria : IRepositorioTiposCambio
{
    private readonly AlmacenEnMemoria _almacen;

    public RepositorioTiposCambioEnMemoria(AlmacenEnMemoria almacen) => _almacen = almacen;

    public Task<TipoCambio?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        Task.FromResult(_almacen.TiposCambio.Find(t => t.Id == id));

    public Task<TipoCambio?> ObtenerActivoAsync(CancellationToken cancelacion = default) =>
        Task.FromResult(_almacen.TiposCambio
            .OrderByDescending(t => t.FechaVigencia)
            .FirstOrDefault(t => t.Activo));

    public Task<IReadOnlyList<TipoCambio>> ListarActivosAsync(CancellationToken cancelacion = default)
    {
        IReadOnlyList<TipoCambio> activos = _almacen.TiposCambio.Where(t => t.Activo).ToList();
        return Task.FromResult(activos);
    }

    public Task<PaginaResultado<TipoCambio>> ListarAsync(
        ParametrosConsulta parametros,
        CancellationToken cancelacion = default)
    {
        var ordenada = _almacen.TiposCambio.OrderByDescending(t => t.FechaVigencia).ToList();

        return Task.FromResult(new PaginaResultado<TipoCambio>(
            ordenada.Skip(parametros.Omitir).Take(parametros.TamanoPagina).ToList(),
            parametros.Pagina,
            parametros.TamanoPagina,
            ordenada.Count));
    }

    public void Agregar(TipoCambio tipoCambio) => _almacen.TiposCambio.Add(tipoCambio);

    public void Eliminar(TipoCambio tipoCambio) => _almacen.TiposCambio.Remove(tipoCambio);
}
