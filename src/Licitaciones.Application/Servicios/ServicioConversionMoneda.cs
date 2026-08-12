using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Servicios;

namespace Licitaciones.Application.Servicios;

/// <summary>
/// Implementación de la conversión referencial de colones a dólares.
/// </summary>
/// <remarks>
/// El valor en colones nunca se modifica: la conversión es solo una representación calculada que se
/// muestra junto a la fecha del tipo de cambio utilizado (sección 8.8).
/// </remarks>
public sealed class ServicioConversionMoneda : IServicioConversionMoneda
{
    private readonly IRepositorioTiposCambio _repositorio;

    /// <summary>
    /// Crea el servicio con sus dependencias.
    /// </summary>
    /// <param name="repositorio">Puerto de acceso a tipos de cambio.</param>
    public ServicioConversionMoneda(IRepositorioTiposCambio repositorio) => _repositorio = repositorio;

    /// <inheritdoc />
    public async Task<Resultado<MontoConvertidoDto>> ConvertirAsync(
        decimal montoCrc,
        CancellationToken cancelacion = default)
    {
        TipoCambio? activo = await _repositorio.ObtenerActivoAsync(cancelacion);

        if (activo is null)
        {
            return Resultado<MontoConvertidoDto>.Fallido(ErrorApp.Conflicto(
                CodigosError.TipoCambioActivoRequerido,
                "No hay un tipo de cambio activo configurado. Registre uno para ver montos en dólares."));
        }

        decimal montoUsd = ConversorMoneda.ConvertirACrcAUsd(montoCrc, activo.CrcPorUsd);

        return Resultado<MontoConvertidoDto>.Exitoso(
            new MontoConvertidoDto(montoCrc, montoUsd, activo.CrcPorUsd, activo.FechaVigencia));
    }
}
