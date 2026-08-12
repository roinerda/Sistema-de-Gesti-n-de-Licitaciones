using Licitaciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

/// <summary>
/// Mapeo relacional de <see cref="NivelAprobacion"/>.
/// </summary>
public sealed class NivelAprobacionConfiguracion : IEntityTypeConfiguration<NivelAprobacion>
{
    /// <summary>Identificador fijo del nivel «Encargado de área» usado en los datos semilla.</summary>
    public static readonly Guid NivelEncargadoAreaId = new("6f1f4d3e-0f2a-4c5d-9c6b-1a2b3c4d5e01");

    /// <summary>Identificador fijo del nivel «Gerencia» usado en los datos semilla.</summary>
    public static readonly Guid NivelGerenciaId = new("6f1f4d3e-0f2a-4c5d-9c6b-1a2b3c4d5e02");

    /// <summary>Identificador fijo del nivel «Junta Directiva» usado en los datos semilla.</summary>
    public static readonly Guid NivelJuntaDirectivaId = new("6f1f4d3e-0f2a-4c5d-9c6b-1a2b3c4d5e03");

    private static readonly DateTimeOffset FechaSemilla = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NivelAprobacion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("niveles_aprobacion", tabla =>
        {
            tabla.HasCheckConstraint("ck_niveles_aprobacion_minimo_positivo", "monto_minimo_crc > 0");
            tabla.HasCheckConstraint(
                "ck_niveles_aprobacion_rango_coherente",
                "monto_maximo_crc IS NULL OR monto_maximo_crc >= monto_minimo_crc");
        });

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(n => n.MontoMinimoCrc)
            .HasColumnName("monto_minimo_crc")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(n => n.MontoMaximoCrc)
            .HasColumnName("monto_maximo_crc")
            .HasPrecision(18, 2);

        builder.Property(n => n.Aprobador)
            .HasColumnName("aprobador")
            .HasMaxLength(NivelAprobacion.LongitudMaximaAprobador)
            .IsRequired();

        builder.Property(n => n.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(n => n.UpdatedAt).HasColumnName("updated_at").IsRequired();

        ConfiguracionConcurrencia.MapearVersion(builder);

        builder.HasIndex(n => n.MontoMinimoCrc)
            .HasDatabaseName("ux_niveles_aprobacion_monto_minimo")
            .IsUnique();

        // Datos semilla de la tabla de aprobación definida en la sección 8.7 del enunciado.
        builder.HasData(
            new
            {
                Id = NivelEncargadoAreaId,
                MontoMinimoCrc = 0.01m,
                MontoMaximoCrc = (decimal?)999_999.99m,
                Aprobador = "Encargado de área",
                CreatedAt = FechaSemilla,
                UpdatedAt = FechaSemilla,
                Version = 1,
            },
            new
            {
                Id = NivelGerenciaId,
                MontoMinimoCrc = 1_000_000.00m,
                MontoMaximoCrc = (decimal?)9_999_999.99m,
                Aprobador = "Gerencia",
                CreatedAt = FechaSemilla,
                UpdatedAt = FechaSemilla,
                Version = 1,
            },
            new
            {
                Id = NivelJuntaDirectivaId,
                MontoMinimoCrc = 10_000_000.00m,
                MontoMaximoCrc = (decimal?)null,
                Aprobador = "Junta Directiva",
                CreatedAt = FechaSemilla,
                UpdatedAt = FechaSemilla,
                Version = 1,
            });
    }
}
