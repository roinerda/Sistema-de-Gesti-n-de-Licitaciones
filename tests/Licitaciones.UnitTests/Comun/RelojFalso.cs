using Licitaciones.Domain.Comun;

namespace Licitaciones.UnitTests.Comun;

/// <summary>
/// Reloj controlado por la prueba.
/// </summary>
/// <remarks>
/// Permite verificar reglas de vencimiento sin esperas reales: la prueba adelanta el tiempo y comprueba que
/// el sistema reacciona. Es la razón por la que el dominio nunca lee <c>DateTimeOffset.UtcNow</c>.
/// </remarks>
public sealed class RelojFalso : IReloj
{
    public RelojFalso(DateTimeOffset? inicial = null) =>
        Ahora = inicial ?? new DateTimeOffset(2026, 3, 2, 8, 0, 0, TimeSpan.Zero);

    public DateTimeOffset Ahora { get; set; }

    public void Avanzar(TimeSpan intervalo) => Ahora = Ahora.Add(intervalo);
}
