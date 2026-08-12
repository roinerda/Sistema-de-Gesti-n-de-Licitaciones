using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
using Licitaciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Persistencia.Repositorios;

/// <summary>
/// Acceso a niveles de aprobación sobre Entity Framework Core.
/// </summary>
public sealed class RepositorioNivelesAprobacion : IRepositorioNivelesAprobacion
{
    private readonly LicitacionesDbContext _contexto;

    /// <summary>
    /// Crea el repositorio.
    /// </summary>
    /// <param name="contexto">Contexto de Entity Framework Core.</param>
    public RepositorioNivelesAprobacion(LicitacionesDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    public Task<NivelAprobacion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        _contexto.NivelesAprobacion.FirstOrDefaultAsync(n => n.Id == id, cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<NivelAprobacion>> ListarTodosAsync(CancellationToken cancelacion = default) =>
        await _contexto.NivelesAprobacion
            .OrderBy(n => n.MontoMinimoCrc)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<PaginaResultado<NivelAprobacion>> ListarAsync(
        ParametrosConsulta parametros,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(parametros);

        IQueryable<NivelAprobacion> consulta = _contexto.NivelesAprobacion.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(parametros.Buscar))
        {
            string patron = PatronesBusqueda.ParaContiene(parametros.Buscar.Trim());
            consulta = consulta.Where(n => EF.Functions.ILike(n.Aprobador, patron));
        }

        consulta = parametros.OrdenarPor?.ToLowerInvariant() switch
        {
            "aprobador" => parametros.Descendente
                ? consulta.OrderByDescending(n => n.Aprobador)
                : consulta.OrderBy(n => n.Aprobador),
            _ => parametros.Descendente
                ? consulta.OrderByDescending(n => n.MontoMinimoCrc)
                : consulta.OrderBy(n => n.MontoMinimoCrc),
        };

        int total = await consulta.CountAsync(cancelacion);

        List<NivelAprobacion> elementos = await consulta
            .Skip(parametros.Omitir)
            .Take(parametros.TamanoPagina)
            .ToListAsync(cancelacion);

        return new PaginaResultado<NivelAprobacion>(elementos, parametros.Pagina, parametros.TamanoPagina, total);
    }

    /// <inheritdoc />
    public void Agregar(NivelAprobacion nivel) => _contexto.NivelesAprobacion.Add(nivel);

    /// <inheritdoc />
    public void Eliminar(NivelAprobacion nivel) => _contexto.NivelesAprobacion.Remove(nivel);
}

/// <summary>
/// Acceso a tipos de cambio sobre Entity Framework Core.
/// </summary>
public sealed class RepositorioTiposCambio : IRepositorioTiposCambio
{
    private readonly LicitacionesDbContext _contexto;

    /// <summary>
    /// Crea el repositorio.
    /// </summary>
    /// <param name="contexto">Contexto de Entity Framework Core.</param>
    public RepositorioTiposCambio(LicitacionesDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    public Task<TipoCambio?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        _contexto.TiposCambio.FirstOrDefaultAsync(t => t.Id == id, cancelacion);

    /// <inheritdoc />
    public Task<TipoCambio?> ObtenerActivoAsync(CancellationToken cancelacion = default) =>
        _contexto.TiposCambio
            .AsNoTracking()
            .OrderByDescending(t => t.FechaVigencia)
            .FirstOrDefaultAsync(t => t.Activo, cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<TipoCambio>> ListarActivosAsync(CancellationToken cancelacion = default) =>
        await _contexto.TiposCambio.Where(t => t.Activo).ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<PaginaResultado<TipoCambio>> ListarAsync(
        ParametrosConsulta parametros,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(parametros);

        IQueryable<TipoCambio> consulta = _contexto.TiposCambio.AsNoTracking();

        consulta = parametros.OrdenarPor?.ToLowerInvariant() switch
        {
            "valor" => parametros.Descendente
                ? consulta.OrderByDescending(t => t.CrcPorUsd)
                : consulta.OrderBy(t => t.CrcPorUsd),
            "activo" => parametros.Descendente
                ? consulta.OrderByDescending(t => t.Activo).ThenByDescending(t => t.FechaVigencia)
                : consulta.OrderBy(t => t.Activo).ThenByDescending(t => t.FechaVigencia),
            _ => parametros.Descendente
                ? consulta.OrderByDescending(t => t.FechaVigencia)
                : consulta.OrderBy(t => t.FechaVigencia),
        };

        int total = await consulta.CountAsync(cancelacion);

        List<TipoCambio> elementos = await consulta
            .Skip(parametros.Omitir)
            .Take(parametros.TamanoPagina)
            .ToListAsync(cancelacion);

        return new PaginaResultado<TipoCambio>(elementos, parametros.Pagina, parametros.TamanoPagina, total);
    }

    /// <inheritdoc />
    public void Agregar(TipoCambio tipoCambio) => _contexto.TiposCambio.Add(tipoCambio);

    /// <inheritdoc />
    public void Eliminar(TipoCambio tipoCambio) => _contexto.TiposCambio.Remove(tipoCambio);
}
