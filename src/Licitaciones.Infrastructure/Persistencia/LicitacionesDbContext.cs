using System.Reflection;
using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Licitaciones.Infrastructure.Persistencia;

/// <summary>
/// Contexto de Entity Framework Core sobre PostgreSQL.
/// </summary>
public sealed class LicitacionesDbContext : DbContext
{
    private readonly IReloj _reloj;

    /// <summary>
    /// Crea el contexto.
    /// </summary>
    /// <param name="opciones">Opciones de configuración del contexto.</param>
    /// <param name="reloj">Reloj inyectable usado para las marcas de auditoría.</param>
    public LicitacionesDbContext(DbContextOptions<LicitacionesDbContext> opciones, IReloj reloj)
        : base(opciones) => _reloj = reloj;

    /// <summary>Licitaciones registradas.</summary>
    public DbSet<Licitacion> Licitaciones => Set<Licitacion>();

    /// <summary>Proveedores registrados.</summary>
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();

    /// <summary>Ofertas registradas.</summary>
    public DbSet<Oferta> Ofertas => Set<Oferta>();

    /// <summary>Niveles de aprobación configurados.</summary>
    public DbSet<NivelAprobacion> NivelesAprobacion => Set<NivelAprobacion>();

    /// <summary>Tipos de cambio administrados.</summary>
    public DbSet<TipoCambio> TiposCambio => Set<TipoCambio>();

    /// <summary>Catálogo de estados de licitación.</summary>
    public DbSet<CatalogoEstadoLicitacion> EstadosLicitacion => Set<CatalogoEstadoLicitacion>();

    /// <inheritdoc />
    public override int SaveChanges()
    {
        ActualizarMarcasDeAuditoria();
        return base.SaveChanges();
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ActualizarMarcasDeAuditoria();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Refresca <c>UpdatedAt</c> como red de seguridad.
    /// </summary>
    /// <remarks>
    /// Los métodos de dominio ya actualizan la marca; esto cubre el caso de una modificación aplicada por
    /// una ruta que no pase por ellos y evita que el campo quede desfasado.
    /// </remarks>
    private void ActualizarMarcasDeAuditoria()
    {
        DateTimeOffset ahora = _reloj.Ahora.ToUniversalTime();

        foreach (EntityEntry<EntidadBase> entrada in ChangeTracker.Entries<EntidadBase>())
        {
            switch (entrada.State)
            {
                case EntityState.Added:
                    if (entrada.Entity.CreatedAt == default)
                    {
                        entrada.Property(e => e.CreatedAt).CurrentValue = ahora;
                    }

                    entrada.Property(e => e.UpdatedAt).CurrentValue = ahora;
                    break;

                case EntityState.Modified:
                    entrada.Property(e => e.UpdatedAt).CurrentValue = ahora;
                    entrada.Property(e => e.CreatedAt).IsModified = false;
                    break;

                default:
                    break;
            }
        }
    }
}
