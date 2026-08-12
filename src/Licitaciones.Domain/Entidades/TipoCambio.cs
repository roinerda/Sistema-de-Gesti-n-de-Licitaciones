using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Excepciones;

namespace Licitaciones.Domain.Entidades;

/// <summary>
/// Tipo de cambio administrable que expresa cuántos colones equivalen a un dólar estadounidense.
/// </summary>
/// <remarks>
/// El sistema debe funcionar sin acceso a Internet, por lo que el tipo de cambio se administra localmente.
/// Solo un registro puede estar activo para la operación ordinaria (sección 8.8).
/// </remarks>
public sealed class TipoCambio : EntidadBase
{
    /// <summary>Constructor sin parámetros requerido por Entity Framework Core.</summary>
    private TipoCambio()
    {
    }

    /// <summary>Colones por cada dólar estadounidense. Siempre mayor que cero.</summary>
    public decimal CrcPorUsd { get; private set; }

    /// <summary>Fecha a partir de la cual rige el tipo de cambio.</summary>
    public DateTimeOffset FechaVigencia { get; private set; }

    /// <summary>Indica si es el tipo de cambio vigente para la operación ordinaria.</summary>
    public bool Activo { get; private set; }

    /// <summary>
    /// Crea un tipo de cambio.
    /// </summary>
    /// <param name="crcPorUsd">Colones por dólar; debe ser mayor que cero.</param>
    /// <param name="fechaVigencia">Fecha de vigencia.</param>
    /// <param name="activo">Indica si queda activo al crearse.</param>
    /// <param name="ahora">Instante actual provisto por <see cref="IReloj"/>.</param>
    /// <returns>Tipo de cambio válido, aún no persistido.</returns>
    /// <exception cref="ReglaNegocioException">Si el valor no es mayor que cero.</exception>
    public static TipoCambio Crear(decimal crcPorUsd, DateTimeOffset fechaVigencia, bool activo, DateTimeOffset ahora)
    {
        var tipoCambio = new TipoCambio();
        tipoCambio.InicializarAuditoria(ahora);
        tipoCambio.AsignarValores(crcPorUsd, fechaVigencia);
        tipoCambio.Activo = activo;
        return tipoCambio;
    }

    /// <summary>
    /// Actualiza el valor y la fecha de vigencia.
    /// </summary>
    /// <param name="crcPorUsd">Nuevo valor de colones por dólar.</param>
    /// <param name="fechaVigencia">Nueva fecha de vigencia.</param>
    /// <param name="ahora">Instante actual provisto por <see cref="IReloj"/>.</param>
    /// <exception cref="ReglaNegocioException">Si el valor no es mayor que cero.</exception>
    public void Actualizar(decimal crcPorUsd, DateTimeOffset fechaVigencia, DateTimeOffset ahora)
    {
        AsignarValores(crcPorUsd, fechaVigencia);
        RegistrarActualizacion(ahora);
    }

    /// <summary>
    /// Marca este tipo de cambio como el activo.
    /// </summary>
    /// <param name="ahora">Instante actual provisto por <see cref="IReloj"/>.</param>
    public void Activar(DateTimeOffset ahora)
    {
        if (Activo)
        {
            return;
        }

        Activo = true;
        RegistrarActualizacion(ahora);
    }

    /// <summary>
    /// Retira la marca de activo.
    /// </summary>
    /// <param name="ahora">Instante actual provisto por <see cref="IReloj"/>.</param>
    public void Desactivar(DateTimeOffset ahora)
    {
        if (!Activo)
        {
            return;
        }

        Activo = false;
        RegistrarActualizacion(ahora);
    }

    private void AsignarValores(decimal crcPorUsd, DateTimeOffset fechaVigencia)
    {
        if (crcPorUsd <= 0m)
        {
            throw new ReglaNegocioException(
                CodigosError.TipoCambioInvalido,
                "El tipo de cambio debe ser mayor que cero.",
                nameof(CrcPorUsd));
        }

        CrcPorUsd = decimal.Round(crcPorUsd, 4, MidpointRounding.AwayFromZero);
        FechaVigencia = fechaVigencia.ToUniversalTime();
    }
}
