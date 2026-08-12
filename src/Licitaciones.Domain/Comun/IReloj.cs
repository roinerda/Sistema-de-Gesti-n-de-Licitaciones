namespace Licitaciones.Domain.Comun;

/// <summary>
/// Abstracción del reloj del sistema.
/// </summary>
/// <remarks>
/// El dominio nunca consulta <see cref="DateTimeOffset.UtcNow"/> de forma directa: todas las reglas
/// dependientes del tiempo (vencimiento de licitaciones, publicación, registro de ofertas) reciben el
/// instante actual desde esta abstracción. Así las pruebas pueden ser deterministas, tal como exige el
/// enunciado en la sección 8.2.
/// </remarks>
public interface IReloj
{
    /// <summary>
    /// Obtiene el instante actual. Siempre se expone en UTC para que las comparaciones internas
    /// sean independientes de la zona horaria del servidor.
    /// </summary>
    DateTimeOffset Ahora { get; }
}
