using Licitaciones.Domain.Comun;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

/// <summary>
/// Mapeo del testigo de concurrencia optimista común a todas las entidades.
/// </summary>
/// <remarks>
/// La columna <c>version</c> se declara como testigo de concurrencia: Entity Framework Core la incluye en la
/// cláusula <c>WHERE</c> de cada <c>UPDATE</c> con el valor original leído. Si otra transacción ya modificó la
/// fila, la actualización no afecta ninguna fila y se lanza <c>DbUpdateConcurrencyException</c>, que la unidad
/// de trabajo traduce a un conflicto controlado.
/// </remarks>
public static class ConfiguracionConcurrencia
{
    /// <summary>
    /// Mapea la propiedad <see cref="EntidadBase.Version"/> a la columna <c>version</c>.
    /// </summary>
    /// <typeparam name="T">Tipo de entidad configurada.</typeparam>
    /// <param name="builder">Constructor de configuración de la entidad.</param>
    public static void MapearVersion<T>(EntityTypeBuilder<T> builder)
        where T : EntidadBase
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property(e => e.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .IsRequired();
    }
}
