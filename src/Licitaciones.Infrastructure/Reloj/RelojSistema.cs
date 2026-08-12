using Licitaciones.Domain.Comun;

namespace Licitaciones.Infrastructure.Reloj;

/// <summary>
/// Implementación real de <see cref="IReloj"/> basada en el reloj del sistema operativo.
/// </summary>
/// <remarks>
/// Es la única clase de la solución autorizada a leer la hora del sistema. Las pruebas sustituyen esta
/// implementación por un reloj controlado para verificar vencimientos de forma determinista.
/// </remarks>
public sealed class RelojSistema : IReloj
{
    /// <inheritdoc />
    public DateTimeOffset Ahora => DateTimeOffset.UtcNow;
}
