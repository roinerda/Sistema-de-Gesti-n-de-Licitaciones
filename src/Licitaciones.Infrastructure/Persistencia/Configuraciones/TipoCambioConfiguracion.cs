using Licitaciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

/// <summary>
/// Mapeo relacional de <see cref="TipoCambio"/>.
/// </summary>
public sealed class TipoCambioConfiguracion : IEntityTypeConfiguration<TipoCambio>
{
    /// <summary>Identificador fijo del tipo de cambio inicial usado en los datos semilla.</summary>
    public static readonly Guid TipoCambioInicialId = new("8a2b6c4d-1e3f-4a5b-8c9d-0e1f2a3b4c05");

    private static readonly DateTimeOffset FechaSemilla = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TipoCambio> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("tipos_cambio", tabla =>
            tabla.HasCheckConstraint("ck_tipos_cambio_valor_positivo", "crc_por_usd > 0"));

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(t => t.CrcPorUsd)
            .HasColumnName("crc_por_usd")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(t => t.FechaVigencia).HasColumnName("fecha_vigencia").IsRequired();
        builder.Property(t => t.Activo).HasColumnName("activo").IsRequired();
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").IsRequired();

        ConfiguracionConcurrencia.MapearVersion(builder);

        // Índice único parcial: a lo sumo una fila puede tener activo = true.
        builder.HasIndex(t => t.Activo)
            .HasDatabaseName("ux_tipos_cambio_activo")
            .IsUnique()
            .HasFilter("activo");

        builder.HasIndex(t => t.FechaVigencia).HasDatabaseName("ix_tipos_cambio_fecha_vigencia");

        // Tipo de cambio inicial: permite operar sin Internet desde la primera ejecución.
        builder.HasData(new
        {
            Id = TipoCambioInicialId,
            CrcPorUsd = 520.0000m,
            FechaVigencia = FechaSemilla,
            Activo = true,
            CreatedAt = FechaSemilla,
            UpdatedAt = FechaSemilla,
            Version = 1,
        });
    }
}
