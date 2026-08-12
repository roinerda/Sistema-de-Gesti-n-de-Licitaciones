using Licitaciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

/// <summary>
/// Mapeo relacional de <see cref="Proveedor"/>.
/// </summary>
public sealed class ProveedorConfiguracion : IEntityTypeConfiguration<Proveedor>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Proveedor> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("proveedores", tabla =>
            tabla.HasCheckConstraint("ck_proveedores_nombre_no_vacio", "length(btrim(nombre)) > 0"));

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(Proveedor.LongitudMaximaNombre)
            .IsRequired();

        builder.Property(p => p.NombreNormalizado)
            .HasColumnName("nombre_normalizado")
            .HasMaxLength(Proveedor.LongitudMaximaNombre)
            .IsRequired();

        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");

        ConfiguracionConcurrencia.MapearVersion(builder);

        // Índice único parcial: la unicidad aplica solo entre proveedores vigentes, de modo que
        // un nombre liberado por un borrado lógico pueda volver a utilizarse.
        builder.HasIndex(p => p.NombreNormalizado)
            .HasDatabaseName("ux_proveedores_nombre_normalizado")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");
    }
}
