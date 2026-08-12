using Licitaciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

/// <summary>
/// Mapeo relacional de <see cref="Licitacion"/>.
/// </summary>
public sealed class LicitacionConfiguracion : IEntityTypeConfiguration<Licitacion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Licitacion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("licitaciones", tabla =>
        {
            // El presupuesto positivo se valida en interfaz, servidor y también aquí: la base de datos
            // es la última línea de defensa ante cualquier escritura que evada la aplicación.
            tabla.HasCheckConstraint("ck_licitaciones_presupuesto_positivo", "presupuesto_estimado_crc > 0");
            tabla.HasCheckConstraint("ck_licitaciones_codigo_no_vacio", "length(btrim(codigo)) > 0");
        });

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(l => l.Codigo)
            .HasColumnName("codigo")
            .HasMaxLength(Licitacion.LongitudMaximaCodigo)
            .IsRequired();

        builder.Property(l => l.CodigoNormalizado)
            .HasColumnName("codigo_normalizado")
            .HasMaxLength(Licitacion.LongitudMaximaCodigo)
            .IsRequired();

        builder.Property(l => l.Titulo)
            .HasColumnName("titulo")
            .HasMaxLength(Licitacion.LongitudMaximaTitulo)
            .IsRequired();

        builder.Property(l => l.Estado)
            .HasColumnName("estado")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(l => l.FechaCierre).HasColumnName("fecha_cierre").IsRequired();

        // numeric(18,2): nunca se usan float ni double para valores monetarios.
        builder.Property(l => l.PresupuestoEstimadoCrc)
            .HasColumnName("presupuesto_estimado_crc")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(l => l.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(l => l.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(l => l.DeletedAt).HasColumnName("deleted_at");

        ConfiguracionConcurrencia.MapearVersion(builder);

        builder.HasIndex(l => l.CodigoNormalizado)
            .HasDatabaseName("ux_licitaciones_codigo_normalizado")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(l => l.Estado).HasDatabaseName("ix_licitaciones_estado");
        builder.HasIndex(l => l.FechaCierre).HasDatabaseName("ix_licitaciones_fecha_cierre");

        // Clave foránea al catálogo de estados: la integridad del ciclo de vida también se valida en la base.
        builder.HasOne<CatalogoEstadoLicitacion>()
            .WithMany()
            .HasForeignKey(l => l.Estado)
            .HasConstraintName("fk_licitaciones_estado")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
