using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Normalizacion;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Persistencia.Repositorios;

/// <summary>
/// Acceso a licitaciones sobre Entity Framework Core.
/// </summary>
public sealed class RepositorioLicitaciones : IRepositorioLicitaciones
{
    private readonly LicitacionesDbContext _contexto;

    /// <summary>
    /// Crea el repositorio.
    /// </summary>
    /// <param name="contexto">Contexto de Entity Framework Core.</param>
    public RepositorioLicitaciones(LicitacionesDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    public Task<Licitacion?> ObtenerPorIdAsync(
        Guid id,
        bool incluirEliminadas = false,
        CancellationToken cancelacion = default) =>
        _contexto.Licitaciones
            .Where(l => l.Id == id && (incluirEliminadas || l.DeletedAt == null))
            .FirstOrDefaultAsync(cancelacion);

    /// <inheritdoc />
    public Task<bool> ExisteCodigoAsync(
        string codigoNormalizado,
        Guid? idExcluido = null,
        CancellationToken cancelacion = default) =>
        _contexto.Licitaciones
            .AsNoTracking()
            .AnyAsync(
                l => l.CodigoNormalizado == codigoNormalizado
                    && l.DeletedAt == null
                    && (idExcluido == null || l.Id != idExcluido),
                cancelacion);

    /// <inheritdoc />
    public async Task<PaginaResultado<Licitacion>> ListarAsync(
        ParametrosConsultaLicitaciones parametros,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(parametros);

        IQueryable<Licitacion> consulta = _contexto.Licitaciones.AsNoTracking();

        if (!parametros.IncluirEliminadas)
        {
            consulta = consulta.Where(l => l.DeletedAt == null);
        }

        if (parametros.Estado is { } estado)
        {
            consulta = consulta.Where(l => l.Estado == estado);
        }

        if (!string.IsNullOrWhiteSpace(parametros.Buscar))
        {
            // El código se compara sobre la columna normalizada; el título usa ILIKE para que la
            // búsqueda ignore mayúsculas sin depender de la cultura del servidor.
            string patronCodigo = NormalizadorTexto.NormalizarCodigo(parametros.Buscar);
            string patronTitulo = PatronesBusqueda.ParaContiene(parametros.Buscar.Trim());

            consulta = consulta.Where(l =>
                l.CodigoNormalizado.Contains(patronCodigo) || EF.Functions.ILike(l.Titulo, patronTitulo));
        }

        consulta = parametros.OrdenarPor?.ToLowerInvariant() switch
        {
            "codigo" => parametros.Descendente
                ? consulta.OrderByDescending(l => l.CodigoNormalizado)
                : consulta.OrderBy(l => l.CodigoNormalizado),
            "titulo" => parametros.Descendente
                ? consulta.OrderByDescending(l => l.Titulo)
                : consulta.OrderBy(l => l.Titulo),
            "presupuesto" => parametros.Descendente
                ? consulta.OrderByDescending(l => l.PresupuestoEstimadoCrc)
                : consulta.OrderBy(l => l.PresupuestoEstimadoCrc),
            "estado" => parametros.Descendente
                ? consulta.OrderByDescending(l => l.Estado)
                : consulta.OrderBy(l => l.Estado),
            "creado" => parametros.Descendente
                ? consulta.OrderByDescending(l => l.CreatedAt)
                : consulta.OrderBy(l => l.CreatedAt),
            _ => parametros.Descendente
                ? consulta.OrderByDescending(l => l.FechaCierre)
                : consulta.OrderBy(l => l.FechaCierre),
        };

        int total = await consulta.CountAsync(cancelacion);

        List<Licitacion> elementos = await consulta
            .Skip(parametros.Omitir)
            .Take(parametros.TamanoPagina)
            .ToListAsync(cancelacion);

        return new PaginaResultado<Licitacion>(elementos, parametros.Pagina, parametros.TamanoPagina, total);
    }

    /// <inheritdoc />
    public async Task<decimal?> ObtenerMontoOfertaMayorAsync(Guid licitacionId, CancellationToken cancelacion = default)
    {
        // MaxAsync sobre decimal? devuelve null cuando la licitación no tiene ofertas.
        return await _contexto.Ofertas
            .AsNoTracking()
            .Where(o => o.LicitacionId == licitacionId)
            .MaxAsync(o => (decimal?)o.MontoOfertadoCrc, cancelacion);
    }

    /// <inheritdoc />
    public Task<int> ContarOfertasAsync(Guid licitacionId, CancellationToken cancelacion = default) =>
        _contexto.Ofertas.AsNoTracking().CountAsync(o => o.LicitacionId == licitacionId, cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> ContarOfertasAsync(
        IReadOnlyCollection<Guid> licitacionesIds,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(licitacionesIds);

        if (licitacionesIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var conteos = await _contexto.Ofertas
            .AsNoTracking()
            .Where(o => licitacionesIds.Contains(o.LicitacionId))
            .GroupBy(o => o.LicitacionId)
            .Select(g => new { LicitacionId = g.Key, Total = g.Count() })
            .ToListAsync(cancelacion);

        return conteos.ToDictionary(c => c.LicitacionId, c => c.Total);
    }

    /// <inheritdoc />
    public void Agregar(Licitacion licitacion) => _contexto.Licitaciones.Add(licitacion);
}
