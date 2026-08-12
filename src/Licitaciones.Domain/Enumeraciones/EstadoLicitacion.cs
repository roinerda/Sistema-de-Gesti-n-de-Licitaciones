namespace Licitaciones.Domain.Enumeraciones;

/// <summary>
/// Estados del ciclo de vida de una licitación (sección 8.1 del enunciado).
/// </summary>
public enum EstadoLicitacion
{
    /// <summary>Licitación en preparación; admite edición y no acepta ofertas.</summary>
    Borrador = 0,

    /// <summary>Licitación publicada; acepta ofertas mientras no se alcance la fecha de cierre.</summary>
    Publicada = 1,

    /// <summary>Licitación cerrada; es un estado terminal.</summary>
    Cerrada = 2,
}
