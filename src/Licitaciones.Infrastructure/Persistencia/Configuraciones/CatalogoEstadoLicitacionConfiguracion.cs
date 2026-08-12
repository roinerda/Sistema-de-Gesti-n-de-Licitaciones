using Licitaciones.Domain.Enumeraciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

/// <summary>
/// Mapeo y datos semilla del catálogo de estados de licitación.
/// </summary>
public sealed class CatalogoEstadoLicitacionConfiguracion : IEntityTypeConfiguration<CatalogoEstadoLicitacion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CatalogoEstadoLicitacion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("estados_licitacion");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasConversion<int>().ValueGeneratedNever();
        builder.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(40).IsRequired();
        builder.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(200).IsRequired();

        builder.HasIndex(e => e.Nombre).HasDatabaseName("ux_estados_licitacion_nombre").IsUnique();

        builder.HasData(
            new CatalogoEstadoLicitacion
            {
                Id = EstadoLicitacion.Borrador,
                Nombre = nameof(EstadoLicitacion.Borrador),
                Descripcion = "Licitación en preparación; admite edición y no acepta ofertas.",
            },
            new CatalogoEstadoLicitacion
            {
                Id = EstadoLicitacion.Publicada,
                Nombre = nameof(EstadoLicitacion.Publicada),
                Descripcion = "Licitación publicada; acepta ofertas hasta la fecha de cierre.",
            },
            new CatalogoEstadoLicitacion
            {
                Id = EstadoLicitacion.Cerrada,
                Nombre = nameof(EstadoLicitacion.Cerrada),
                Descripcion = "Licitación cerrada; estado terminal que conserva las ofertas como evidencia.",
            });
    }
}
