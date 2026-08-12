using Licitaciones.Domain.Enumeraciones;

namespace Licitaciones.Infrastructure.Persistencia;

/// <summary>
/// Catálogo persistido de estados de licitación.
/// </summary>
/// <remarks>
/// Existe para que la base de datos también garantice la integridad del estado mediante una clave foránea
/// y para cumplir el requisito de datos semilla de estados (sección 11). El dominio sigue trabajando con la
/// enumeración <see cref="Domain.Enumeraciones.EstadoLicitacion" />; esta clase es un detalle de persistencia.
/// </remarks>
public sealed class CatalogoEstadoLicitacion
{
    /// <summary>
    /// Estado catalogado. Se persiste como entero, con el mismo valor de la enumeración de dominio, para
    /// que la clave foránea de <c>licitaciones.estado</c> sea compatible.
    /// </summary>
    public EstadoLicitacion Id { get; set; }

    /// <summary>Nombre del estado.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Descripción funcional del estado.</summary>
    public string Descripcion { get; set; } = string.Empty;
}
