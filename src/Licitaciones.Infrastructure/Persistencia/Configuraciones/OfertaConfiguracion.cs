using Licitaciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

/// <summary>
/// Mapeo relacional de <see cref="Oferta"/>.
/// </summary>
public sealed class OfertaConfiguracion : IEntityTypeConfiguration<Oferta>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Oferta> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ofertas", tabla =>
            tabla.HasCheckConstraint("ck_ofertas_monto_positivo", "monto_ofertado_crc > 0"));

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(o => o.LicitacionId).HasColumnName("licitacion_id").IsRequired();
        builder.Property(o => o.ProveedorId).HasColumnName("proveedor_id").IsRequired();

        builder.Property(o => o.MontoOfertadoCrc)
            .HasColumnName("monto_ofertado_crc")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(o => o.FechaRegistro).HasColumnName("fecha_registro").IsRequired();
        builder.Property(o => o.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at").IsRequired();

        ConfiguracionConcurrencia.MapearVersion(builder);

        // Las relaciones se declaran desde la oferta, que es el lado que posee las claves foráneas.
        // Licitación y proveedor no exponen colecciones de ofertas: la aplicación siempre las consulta
        // por repositorio, con paginación, en lugar de cargar colecciones completas en memoria.
        builder.HasOne(o => o.Licitacion)
            .WithMany()
            .HasForeignKey(o => o.LicitacionId)
            .HasConstraintName("fk_ofertas_licitacion")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Proveedor)
            .WithMany()
            .HasForeignKey(o => o.ProveedorId)
            .HasConstraintName("fk_ofertas_proveedor")
            .OnDelete(DeleteBehavior.Restrict);

        // Índice único compuesto: un proveedor no puede ofertar dos veces en la misma licitación.
        builder.HasIndex(o => new { o.LicitacionId, o.ProveedorId })
            .HasDatabaseName("ux_ofertas_licitacion_proveedor")
            .IsUnique();

        // Apoya la consulta de mejor oferta: menor monto y, en empate, la registrada primero.
        builder.HasIndex(o => new { o.LicitacionId, o.MontoOfertadoCrc, o.FechaRegistro })
            .HasDatabaseName("ix_ofertas_licitacion_monto_fecha");
    }
}
