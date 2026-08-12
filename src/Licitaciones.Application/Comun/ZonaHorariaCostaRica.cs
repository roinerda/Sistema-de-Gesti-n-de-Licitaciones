using System.Globalization;

namespace Licitaciones.Application.Comun;

/// <summary>
/// Utilidades de presentación de fechas y montos en la configuración regional de Costa Rica.
/// </summary>
/// <remarks>
/// Las fechas se almacenan y comparan en UTC; esta clase solo se usa para mostrarlas. Se resuelve primero
/// el identificador IANA <c>America/Costa_Rica</c> y, si el sistema operativo no lo reconoce, se recurre al
/// identificador equivalente de Windows.
/// </remarks>
public static class ZonaHorariaCostaRica
{
    private const string IdentificadorIana = "America/Costa_Rica";
    private const string IdentificadorWindows = "Central America Standard Time";

    private static readonly Lazy<TimeZoneInfo> ZonaHoraria = new(Resolver);

    /// <summary>Cultura usada para el formato monetario y de fechas.</summary>
    public static CultureInfo Cultura { get; } = CultureInfo.GetCultureInfo("es-CR");

    /// <summary>Zona horaria de Costa Rica.</summary>
    public static TimeZoneInfo Zona => ZonaHoraria.Value;

    /// <summary>
    /// Convierte un instante UTC a la hora local de Costa Rica.
    /// </summary>
    /// <param name="instante">Instante en cualquier desplazamiento.</param>
    /// <returns>El mismo instante expresado en la zona horaria de Costa Rica.</returns>
    public static DateTimeOffset AHoraLocal(DateTimeOffset instante) =>
        TimeZoneInfo.ConvertTime(instante, Zona);

    /// <summary>
    /// Interpreta una fecha y hora escrita por la persona usuaria como hora de Costa Rica y la convierte a UTC.
    /// </summary>
    /// <param name="fechaLocal">Fecha y hora sin desplazamiento, tal como la envía un control de calendario.</param>
    /// <returns>Instante equivalente en UTC.</returns>
    public static DateTimeOffset DesdeHoraLocal(DateTime fechaLocal)
    {
        DateTime sinZona = DateTime.SpecifyKind(fechaLocal, DateTimeKind.Unspecified);
        TimeSpan desplazamiento = Zona.GetUtcOffset(sinZona);
        return new DateTimeOffset(sinZona, desplazamiento).ToUniversalTime();
    }

    /// <summary>
    /// Formatea un instante en hora de Costa Rica.
    /// </summary>
    /// <param name="instante">Instante a formatear.</param>
    /// <returns>Texto con fecha y hora local.</returns>
    public static string Formatear(DateTimeOffset instante) =>
        AHoraLocal(instante).ToString("dd/MM/yyyy HH:mm", Cultura);

    /// <summary>
    /// Formatea un monto en colones con el símbolo y los separadores de es-CR.
    /// </summary>
    /// <param name="montoCrc">Monto en colones.</param>
    /// <returns>Texto monetario, por ejemplo <c>₡1 500 000,00</c>.</returns>
    public static string FormatearColones(decimal montoCrc) => montoCrc.ToString("C2", Cultura);

    /// <summary>
    /// Formatea un monto en dólares estadounidenses.
    /// </summary>
    /// <param name="montoUsd">Monto en dólares.</param>
    /// <returns>Texto monetario en formato estadounidense.</returns>
    public static string FormatearDolares(decimal montoUsd) =>
        montoUsd.ToString("C2", CultureInfo.GetCultureInfo("en-US"));

    private static TimeZoneInfo Resolver()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(IdentificadorIana);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(IdentificadorWindows);
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(IdentificadorWindows);
        }
    }
}
