using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Normalizacion;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Persistencia.Repositorios;

/// <summary>
/// Acceso a proveedores sobre Entity Framework Core.
/// </summary>
public sealed class RepositorioProveedores : IRepositorioProveedores
{
    private readonly LicitacionesDbContext _contexto;

    /// <summary>
    /// Crea el repositorio.
    /// </summary>
    /// <param name="contexto">Contexto de Entity Framework Core.</param>
    public RepositorioProveedores(LicitacionesDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    public Task<Proveedor?> ObtenerPorIdAsync(
        Guid id,
        bool incluirEliminados = false,
        CancellationToken cancelacion = default) =>
        _contexto.Proveedores
            .Where(p => p.Id == id && (incluirEliminados || p.DeletedAt == null))
            .FirstOrDefaultAsync(cancelacion);

    /// <inheritdoc />
    public Task<bool> ExisteNombreAsync(
        string nombreNormalizado,
        Guid? idExcluido = null,
        CancellationToken cancelacion = default) =>
        _contexto.Proveedores
            .AsNoTracking()
            .AnyAsync(
                p => p.NombreNormalizado == nombreNormalizado
                    && p.DeletedAt == null
                    && (idExcluido == null || p.Id != idExcluido),
                cancelacion);

    /// <inheritdoc />
    public async Task<PaginaResultado<Proveedor>> ListarAsync(
        ParametrosConsultaProveedores parametros,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(parametros);

        IQueryable<Proveedor> consulta = _contexto.Proveedores.AsNoTracking();

        if (!parametros.IncluirEliminados)
        {
            consulta = consulta.Where(p => p.DeletedAt == null);
        }

        if (!string.IsNullOrWhiteSpace(parametros.Buscar))
        {
            // La búsqueda usa el nombre normalizado: así ignora mayúsculas, espacios repetidos y acentos
            // de composición, igual que la regla de unicidad.
            string patron = NormalizadorTexto.NormalizarNombre(parametros.Buscar);
            consulta = consulta.Where(p => p.NombreNormalizado.Contains(patron));
        }

        consulta = parametros.OrdenarPor?.ToLowerInvariant() switch
        {
            "creado" => parametros.Descendente
                ? consulta.OrderByDescending(p => p.CreatedAt)
                : consulta.OrderBy(p => p.CreatedAt),
            "actualizado" => parametros.Descendente
                ? consulta.OrderByDescending(p => p.UpdatedAt)
                : consulta.OrderBy(p => p.UpdatedAt),
            _ => parametros.Descendente
                ? consulta.OrderByDescending(p => p.NombreNormalizado)
                : consulta.OrderBy(p => p.NombreNormalizado),
        };

        int total = await consulta.CountAsync(cancelacion);

        List<Proveedor> elementos = await consulta
            .Skip(parametros.Omitir)
            .Take(parametros.TamanoPagina)
            .ToListAsync(cancelacion);

        return new PaginaResultado<Proveedor>(elementos, parametros.Pagina, parametros.TamanoPagina, total);
    }

    /// <inheritdoc />
    public Task<int> ContarOfertasAsync(Guid proveedorId, CancellationToken cancelacion = default) =>
        _contexto.Ofertas.AsNoTracking().CountAsync(o => o.ProveedorId == proveedorId, cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> ContarOfertasAsync(
        IReadOnlyCollection<Guid> proveedoresIds,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(proveedoresIds);

        if (proveedoresIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var conteos = await _contexto.Ofertas
            .AsNoTracking()
            .Where(o => proveedoresIds.Contains(o.ProveedorId))
            .GroupBy(o => o.ProveedorId)
            .Select(g => new { ProveedorId = g.Key, Total = g.Count() })
            .ToListAsync(cancelacion);

        return conteos.ToDictionary(c => c.ProveedorId, c => c.Total);
    }

    /// <inheritdoc />
    public void Agregar(Proveedor proveedor) => _contexto.Proveedores.Add(proveedor);
}
