using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Entidades;

namespace Licitaciones.Application.Servicios;

/// <summary>
/// Implementación de los casos de uso de tipos de cambio.
/// </summary>
/// <remarks>
/// La regla «solo puede existir un tipo de cambio activo» se aplica dentro de una transacción: al activar
/// uno, los demás se desactivan en la misma unidad de trabajo (sección 8.8).
/// </remarks>
public sealed class ServicioTiposCambio : IServicioTiposCambio
{
    private readonly IRepositorioTiposCambio _repositorio;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>
    /// Crea el servicio con sus dependencias.
    /// </summary>
    /// <param name="repositorio">Puerto de acceso a tipos de cambio.</param>
    /// <param name="unidadDeTrabajo">Puerto de persistencia transaccional.</param>
    /// <param name="reloj">Reloj inyectable.</param>
    public ServicioTiposCambio(IRepositorioTiposCambio repositorio, IUnidadDeTrabajo unidadDeTrabajo, IReloj reloj)
    {
        _repositorio = repositorio;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<PaginaResultado<TipoCambioDto>> ListarAsync(
        ParametrosConsulta parametros,
        CancellationToken cancelacion = default)
    {
        PaginaResultado<TipoCambio> pagina = await _repositorio.ListarAsync(parametros, cancelacion);
        return pagina.Proyectar(TipoCambioDto.Desde);
    }

    /// <inheritdoc />
    public async Task<Resultado<TipoCambioDto>> ObtenerAsync(Guid id, CancellationToken cancelacion = default)
    {
        TipoCambio? tipoCambio = await _repositorio.ObtenerPorIdAsync(id, cancelacion);

        return tipoCambio is null
            ? Resultado<TipoCambioDto>.Fallido(ErrorApp.NoEncontrado("El tipo de cambio solicitado no existe."))
            : Resultado<TipoCambioDto>.Exitoso(TipoCambioDto.Desde(tipoCambio));
    }

    /// <inheritdoc />
    public async Task<TipoCambioDto?> ObtenerActivoAsync(CancellationToken cancelacion = default)
    {
        TipoCambio? activo = await _repositorio.ObtenerActivoAsync(cancelacion);
        return activo is null ? null : TipoCambioDto.Desde(activo);
    }

    /// <inheritdoc />
    public Task<Resultado<TipoCambioDto>> CrearAsync(GuardarTipoCambioDto datos, CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(datos);

        return ProtectorCasoUso.ProtegerAsync(async () =>
        {
            var tipoCambio = TipoCambio.Crear(datos.CrcPorUsd, datos.FechaVigencia, datos.Activo, _reloj.Ahora);

            TipoCambioDto creado = await _unidadDeTrabajo.EjecutarEnTransaccionAsync(
                async token =>
                {
                    if (datos.Activo)
                    {
                        await DesactivarOtrosAsync(tipoCambio.Id, token);
                    }

                    _repositorio.Agregar(tipoCambio);
                    await _unidadDeTrabajo.GuardarCambiosAsync(token);
                    return TipoCambioDto.Desde(tipoCambio);
                },
                cancelacion);

            return Resultado<TipoCambioDto>.Exitoso(creado);
        });
    }

    /// <inheritdoc />
    public Task<Resultado<TipoCambioDto>> ActualizarAsync(
        Guid id,
        GuardarTipoCambioDto datos,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(datos);

        return ProtectorCasoUso.ProtegerAsync(async () =>
        {
            TipoCambio? tipoCambio = await _repositorio.ObtenerPorIdAsync(id, cancelacion);

            if (tipoCambio is null)
            {
                return Resultado<TipoCambioDto>.Fallido(ErrorApp.NoEncontrado("El tipo de cambio solicitado no existe."));
            }

            TipoCambioDto actualizado = await _unidadDeTrabajo.EjecutarEnTransaccionAsync(
                async token =>
                {
                    if (datos.Version is { } version)
                    {
                        _unidadDeTrabajo.EstablecerVersionOriginal(tipoCambio, version);
                    }

                    tipoCambio.Actualizar(datos.CrcPorUsd, datos.FechaVigencia, _reloj.Ahora);

                    if (datos.Activo)
                    {
                        await DesactivarOtrosAsync(tipoCambio.Id, token);
                        tipoCambio.Activar(_reloj.Ahora);
                    }
                    else
                    {
                        tipoCambio.Desactivar(_reloj.Ahora);
                    }

                    await _unidadDeTrabajo.GuardarCambiosAsync(token);
                    return TipoCambioDto.Desde(tipoCambio);
                },
                cancelacion);

            return Resultado<TipoCambioDto>.Exitoso(actualizado);
        });
    }

    /// <inheritdoc />
    public Task<Resultado<TipoCambioDto>> ActivarAsync(Guid id, CancellationToken cancelacion = default) =>
        ProtectorCasoUso.ProtegerAsync(async () =>
        {
            TipoCambio? tipoCambio = await _repositorio.ObtenerPorIdAsync(id, cancelacion);

            if (tipoCambio is null)
            {
                return Resultado<TipoCambioDto>.Fallido(ErrorApp.NoEncontrado("El tipo de cambio solicitado no existe."));
            }

            TipoCambioDto activado = await _unidadDeTrabajo.EjecutarEnTransaccionAsync(
                async token =>
                {
                    await DesactivarOtrosAsync(id, token);
                    tipoCambio.Activar(_reloj.Ahora);
                    await _unidadDeTrabajo.GuardarCambiosAsync(token);
                    return TipoCambioDto.Desde(tipoCambio);
                },
                cancelacion);

            return Resultado<TipoCambioDto>.Exitoso(activado);
        });

    /// <inheritdoc />
    public Task<Resultado> EliminarAsync(Guid id, CancellationToken cancelacion = default) =>
        ProtectorCasoUso.ProtegerAsync(async () =>
        {
            TipoCambio? tipoCambio = await _repositorio.ObtenerPorIdAsync(id, cancelacion);

            if (tipoCambio is null)
            {
                return Resultado.Fallido(ErrorApp.NoEncontrado("El tipo de cambio solicitado no existe."));
            }

            if (tipoCambio.Activo)
            {
                return Resultado.Fallido(ErrorApp.Conflicto(
                    CodigosError.TipoCambioActivoNoEliminable,
                    "No se puede eliminar el tipo de cambio activo. Active otro registro antes de eliminarlo."));
            }

            _repositorio.Eliminar(tipoCambio);
            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);
            return Resultado.Exitoso();
        });

    /// <summary>
    /// Desactiva todos los tipos de cambio salvo el indicado y confirma esos cambios de inmediato.
    /// </summary>
    /// <remarks>
    /// El guardado intermedio es intencional: la base de datos tiene un índice único parcial que admite una
    /// sola fila con <c>activo = true</c>. Si la desactivación y la activación viajaran en el mismo lote, el
    /// orden de las sentencias podría violar el índice de forma transitoria. Todo ocurre dentro de la misma
    /// transacción, por lo que la operación sigue siendo atómica.
    /// </remarks>
    private async Task DesactivarOtrosAsync(Guid idExcluido, CancellationToken cancelacion)
    {
        IReadOnlyList<TipoCambio> activos = await _repositorio.ListarActivosAsync(cancelacion);
        var porDesactivar = activos.Where(t => t.Id != idExcluido).ToList();

        if (porDesactivar.Count == 0)
        {
            return;
        }

        foreach (TipoCambio activo in porDesactivar)
        {
            activo.Desactivar(_reloj.Ahora);
        }

        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);
    }
}
