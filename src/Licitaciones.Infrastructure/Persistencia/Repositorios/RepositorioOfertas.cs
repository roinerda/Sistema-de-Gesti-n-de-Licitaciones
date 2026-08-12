using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
using Licitaciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Persistencia.Repositorios;

/// <summary>
/// Acceso a ofertas sobre Entity Framework Core.
/// </summary>
public sealed class RepositorioOfertas : IRepositorioOfertas
{
    private readonly LicitacionesDbContext _contexto;

    /// <summary>
    /// Crea el repositorio.
    /// </summary>
    /// <param name="contexto">Contexto de Entity Framework Core.</param>
    public RepositorioOfertas(LicitacionesDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    public Task<Oferta?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        _contexto.Ofertas
            .Include(o => o.Licitacion)
            .Include(o => o.Proveedor)
            .FirstOrDefaultAsync(o => o.Id == id, cancelacion);

    /// <inheritdoc />
    public Task<bool> ExisteOfertaDeProveedorAsync(
        Guid licitacionId,
        Guid proveedorId,
        Guid? idExcluido = null,
        CancellationToken cancelacion = default) =>
        _contexto.Ofertas
            .AsNoTracking()
            .AnyAsync(
                o => o.LicitacionId == licitacionId
                    && o.ProveedorId == proveedorId
                    && (idExcluido == null || o.Id != idExcluido),
                cancelacion);

    /// <inheritdoc />
    public async Task<PaginaResultado<Oferta>> ListarAsync(
        ParametrosConsultaOfertas parametros,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(parametros);

        IQueryable<Oferta> consulta = _contexto.Ofertas
            .AsNoTracking()
            .Include(o => o.Licitacion)
            .Include(o => o.Proveedor);

        if (parametros.LicitacionId is { } licitacionId)
        {
            consulta = consulta.Where(o => o.LicitacionId == licitacionId);
        }

        if (parametros.ProveedorId is { } proveedorId)
        {
            consulta = consulta.Where(o => o.ProveedorId == proveedorId);
        }

        if (!string.IsNullOrWhiteSpace(parametros.Buscar))
        {
            string patron = parametros.Buscar.Trim().ToUpperInvariant();
            consulta = consulta.Where(o =>
                o.Licitacion!.CodigoNormalizado.Contains(patron)
                || o.Proveedor!.NombreNormalizado.Contains(patron));
        }

        consulta = parametros.OrdenarPor?.ToLowerInvariant() switch
        {
            "proveedor" => parametros.Descendente
                ? consulta.OrderByDescending(o => o.Proveedor!.NombreNormalizado)
                : consulta.OrderBy(o => o.Proveedor!.NombreNormalizado),
            "licitacion" => parametros.Descendente
                ? consulta.OrderByDescending(o => o.Licitacion!.CodigoNormalizado)
                : consulta.OrderBy(o => o.Licitacion!.CodigoNormalizado),
            "fecha" => parametros.Descendente
                ? consulta.OrderByDescending(o => o.FechaRegistro)
                : consulta.OrderBy(o => o.FechaRegistro),
            _ => parametros.Descendente
                ? consulta.OrderByDescending(o => o.MontoOfertadoCrc).ThenBy(o => o.FechaRegistro)
                : consulta.OrderBy(o => o.MontoOfertadoCrc).ThenBy(o => o.FechaRegistro),
        };

        int total = await consulta.CountAsync(cancelacion);

        List<Oferta> elementos = await consulta
            .Skip(parametros.Omitir)
            .Take(parametros.TamanoPagina)
            .ToListAsync(cancelacion);

        return new PaginaResultado<Oferta>(elementos, parametros.Pagina, parametros.TamanoPagina, total);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Oferta>> ListarPorLicitacionAsync(
        Guid licitacionId,
        CancellationToken cancelacion = default) =>
        await _contexto.Ofertas
            .AsNoTracking()
            .Include(o => o.Proveedor)
            .Where(o => o.LicitacionId == licitacionId)
            .OrderBy(o => o.MontoOfertadoCrc)
            .ThenBy(o => o.FechaRegistro)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public void Agregar(Oferta oferta) => _contexto.Ofertas.Add(oferta);

    /// <inheritdoc />
    public void Eliminar(Oferta oferta) => _contexto.Ofertas.Remove(oferta);
}
