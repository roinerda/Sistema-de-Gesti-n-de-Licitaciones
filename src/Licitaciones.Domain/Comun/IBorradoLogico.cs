namespace Licitaciones.Domain.Comun;

/// <summary>
/// Marca las entidades que aplican borrado lógico en lugar de eliminación física.
/// </summary>
/// <remarks>
/// Se usa en licitaciones y proveedores porque el enunciado (sección 8.9) prohíbe destruir registros
/// con ofertas relacionadas: las ofertas deben conservarse como evidencia.
/// </remarks>
public interface IBorradoLogico
{
    /// <summary>
    /// Instante en que el registro fue dado de baja lógicamente, o <see langword="null"/> si está vigente.
    /// </summary>
    DateTimeOffset? DeletedAt { get; }
}
