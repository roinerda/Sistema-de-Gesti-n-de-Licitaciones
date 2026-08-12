using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Excepciones;
using Licitaciones.Domain.Normalizacion;

namespace Licitaciones.Domain.Entidades;

/// <summary>
/// Rango de montos en colones asociado a la persona o instancia que debe aprobar la adjudicación.
/// </summary>
/// <remarks>
/// El aprobador se obtiene consultando esta tabla parametrizable y nunca mediante una cadena fija de
/// condiciones <c>if/else</c> (sección 8.7).
/// </remarks>
public sealed class NivelAprobacion : EntidadBase
{
    /// <summary>Longitud máxima del nombre del aprobador.</summary>
    public const int LongitudMaximaAprobador = 120;

    /// <summary>Constructor sin parámetros requerido por Entity Framework Core.</summary>
    private NivelAprobacion()
    {
    }

    /// <summary>Monto mínimo del rango, inclusive.</summary>
    public decimal MontoMinimoCrc { get; private set; }

    /// <summary>Monto máximo del rango, inclusive, o <see langword="null"/> cuando el rango es abierto.</summary>
    public decimal? MontoMaximoCrc { get; private set; }

    /// <summary>Instancia responsable de aprobar los montos comprendidos en el rango.</summary>
    public string Aprobador { get; private set; } = string.Empty;

    /// <summary>Indica si el rango no tiene límite superior.</summary>
    public bool EsRangoAbierto => MontoMaximoCrc is null;

    /// <summary>
    /// Crea un nivel de aprobación validando el rango y el aprobador.
    /// </summary>
    /// <param name="montoMinimoCrc">Monto mínimo, mayor que cero.</param>
    /// <param name="montoMaximoCrc">Monto máximo o <see langword="null"/> para un rango abierto.</param>
    /// <param name="aprobador">Nombre de la instancia aprobadora.</param>
    /// <param name="ahora">Instante actual provisto por <see cref="IReloj"/>.</param>
    /// <returns>Nivel de aprobación válido, aún no persistido.</returns>
    /// <exception cref="ReglaNegocioException">Si el rango es inconsistente o falta el aprobador.</exception>
    public static NivelAprobacion Crear(decimal montoMinimoCrc, decimal? montoMaximoCrc, string? aprobador, DateTimeOffset ahora)
    {
        var nivel = new NivelAprobacion();
        nivel.InicializarAuditoria(ahora);
        nivel.AsignarRango(montoMinimoCrc, montoMaximoCrc);
        nivel.AsignarAprobador(aprobador);
        return nivel;
    }

    /// <summary>
    /// Actualiza el rango y el aprobador.
    /// </summary>
    /// <param name="montoMinimoCrc">Nuevo monto mínimo.</param>
    /// <param name="montoMaximoCrc">Nuevo monto máximo o <see langword="null"/>.</param>
    /// <param name="aprobador">Nuevo aprobador.</param>
    /// <param name="ahora">Instante actual provisto por <see cref="IReloj"/>.</param>
    /// <exception cref="ReglaNegocioException">Si el rango es inconsistente o falta el aprobador.</exception>
    public void Actualizar(decimal montoMinimoCrc, decimal? montoMaximoCrc, string? aprobador, DateTimeOffset ahora)
    {
        AsignarRango(montoMinimoCrc, montoMaximoCrc);
        AsignarAprobador(aprobador);
        RegistrarActualizacion(ahora);
    }

    /// <summary>
    /// Indica si un monto cae dentro del rango, con ambos extremos inclusive.
    /// </summary>
    /// <param name="montoCrc">Monto a evaluar, en colones.</param>
    /// <returns><see langword="true"/> si el monto pertenece al rango.</returns>
    public bool Contiene(decimal montoCrc) =>
        montoCrc >= MontoMinimoCrc && (MontoMaximoCrc is null || montoCrc <= MontoMaximoCrc.Value);

    /// <summary>
    /// Indica si este rango se traslapa con otro. Un rango abierto se trata como infinito superior.
    /// </summary>
    /// <param name="otro">Rango con el que se compara.</param>
    /// <returns><see langword="true"/> si existe al menos un monto contenido por ambos rangos.</returns>
    public bool SeTraslapaCon(NivelAprobacion otro)
    {
        ArgumentNullException.ThrowIfNull(otro);

        decimal maximoPropio = MontoMaximoCrc ?? decimal.MaxValue;
        decimal maximoOtro = otro.MontoMaximoCrc ?? decimal.MaxValue;

        return MontoMinimoCrc <= maximoOtro && otro.MontoMinimoCrc <= maximoPropio;
    }

    private void AsignarRango(decimal montoMinimoCrc, decimal? montoMaximoCrc)
    {
        if (montoMinimoCrc <= 0m)
        {
            throw new ReglaNegocioException(
                CodigosError.RangoAprobacionInvalido,
                "El monto mínimo debe ser mayor que cero.",
                nameof(MontoMinimoCrc));
        }

        if (montoMaximoCrc is { } maximo)
        {
            if (maximo <= 0m)
            {
                throw new ReglaNegocioException(
                    CodigosError.RangoAprobacionInvalido,
                    "El monto máximo debe ser mayor que cero.",
                    nameof(MontoMaximoCrc));
            }

            if (maximo < montoMinimoCrc)
            {
                throw new ReglaNegocioException(
                    CodigosError.RangoAprobacionInvalido,
                    $"El monto máximo ({NormalizadorTexto.FormatearMonto(maximo)}) no puede ser menor que el mínimo ({NormalizadorTexto.FormatearMonto(montoMinimoCrc)}).",
                    nameof(MontoMaximoCrc));
            }
        }

        MontoMinimoCrc = decimal.Round(montoMinimoCrc, 2, MidpointRounding.AwayFromZero);
        MontoMaximoCrc = montoMaximoCrc is { } valor ? decimal.Round(valor, 2, MidpointRounding.AwayFromZero) : null;
    }

    private void AsignarAprobador(string? aprobador)
    {
        string limpio = NormalizadorTexto.LimpiarEspacios(aprobador);

        if (limpio.Length == 0)
        {
            throw new ReglaNegocioException(
                CodigosError.AprobadorRequerido,
                "El nombre del aprobador es obligatorio.",
                nameof(Aprobador));
        }

        if (limpio.Length > LongitudMaximaAprobador)
        {
            throw new ReglaNegocioException(
                CodigosError.AprobadorRequerido,
                $"El nombre del aprobador no puede superar {LongitudMaximaAprobador} caracteres.",
                nameof(Aprobador));
        }

        Aprobador = limpio;
    }
}
