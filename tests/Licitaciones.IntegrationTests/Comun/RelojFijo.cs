using Licitaciones.Domain.Comun;

namespace Licitaciones.IntegrationTests.Comun;

/// <summary>
/// Reloj controlado por la prueba.
/// </summary>
/// <remarks>
/// Las reglas dependientes del tiempo (cierre funcional de una licitación, marcas de auditoría)
/// solo son verificables de forma determinista si el instante «ahora» lo decide la prueba.
/// </remarks>
public sealed class RelojFijo : IReloj
{
    /// <summary>Instante inicial: 15 de marzo de 2026, 10:00 en Costa Rica (UTC-6).</summary>
    public static readonly DateTimeOffset InstanteInicial =
        new(2026, 3, 15, 16, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public DateTimeOffset Ahora { get; private set; } = InstanteInicial;

    /// <summary>Adelanta el reloj.</summary>
    /// <param name="intervalo">Tiempo a avanzar.</param>
    public void Avanzar(TimeSpan intervalo) => Ahora = Ahora.Add(intervalo);
}
