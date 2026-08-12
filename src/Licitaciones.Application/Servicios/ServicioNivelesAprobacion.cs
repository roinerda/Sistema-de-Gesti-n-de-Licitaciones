using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Servicios;

namespace Licitaciones.Application.Servicios;

/// <summary>
/// Implementación de los casos de uso de niveles de aprobación.
/// </summary>
public sealed class ServicioNivelesAprobacion : IServicioNivelesAprobacion
{
    private readonly IRepositorioNivelesAprobacion _repositorio;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>
    /// Crea el servicio con sus dependencias.
    /// </summary>
    /// <param name="repositorio">Puerto de acceso a niveles de aprobación.</param>
    /// <param name="unidadDeTrabajo">Puerto de persistencia transaccional.</param>
    /// <param name="reloj">Reloj inyectable.</param>
    public ServicioNivelesAprobacion(
        IRepositorioNivelesAprobacion repositorio,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _repositorio = repositorio;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<PaginaResultado<NivelAprobacionDto>> ListarAsync(
        ParametrosConsulta parametros,
        CancellationToken cancelacion = default)
    {
        PaginaResultado<NivelAprobacion> pagina = await _repositorio.ListarAsync(parametros, cancelacion);
        return pagina.Proyectar(NivelAprobacionDto.Desde);
    }

    /// <inheritdoc />
    public async Task<Resultado<NivelAprobacionDto>> ObtenerAsync(Guid id, CancellationToken cancelacion = default)
    {
        NivelAprobacion? nivel = await _repositorio.ObtenerPorIdAsync(id, cancelacion);

        return nivel is null
            ? Resultado<NivelAprobacionDto>.Fallido(ErrorApp.NoEncontrado("El nivel de aprobación solicitado no existe."))
            : Resultado<NivelAprobacionDto>.Exitoso(NivelAprobacionDto.Desde(nivel));
    }

    /// <inheritdoc />
    public Task<Resultado<NivelAprobacionDto>> CrearAsync(
        GuardarNivelAprobacionDto datos,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(datos);

        return ProtectorCasoUso.ProtegerAsync(async () =>
        {
            var nivel = NivelAprobacion.Crear(datos.MontoMinimoCrc, datos.MontoMaximoCrc, datos.Aprobador, _reloj.Ahora);
            IReadOnlyList<NivelAprobacion> existentes = await _repositorio.ListarTodosAsync(cancelacion);

            SelectorNivelAprobacion.GarantizarRangoConsistente(existentes, nivel);

            _repositorio.Agregar(nivel);
            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);
            return Resultado<NivelAprobacionDto>.Exitoso(NivelAprobacionDto.Desde(nivel));
        });
    }

    /// <inheritdoc />
    public Task<Resultado<NivelAprobacionDto>> ActualizarAsync(
        Guid id,
        GuardarNivelAprobacionDto datos,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(datos);

        return ProtectorCasoUso.ProtegerAsync(async () =>
        {
            NivelAprobacion? nivel = await _repositorio.ObtenerPorIdAsync(id, cancelacion);

            if (nivel is null)
            {
                return Resultado<NivelAprobacionDto>.Fallido(
                    ErrorApp.NoEncontrado("El nivel de aprobación solicitado no existe."));
            }

            if (datos.Version is { } version)
            {
                _unidadDeTrabajo.EstablecerVersionOriginal(nivel, version);
            }

            nivel.Actualizar(datos.MontoMinimoCrc, datos.MontoMaximoCrc, datos.Aprobador, _reloj.Ahora);

            IReadOnlyList<NivelAprobacion> existentes = await _repositorio.ListarTodosAsync(cancelacion);
            SelectorNivelAprobacion.GarantizarRangoConsistente(existentes, nivel);

            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);
            return Resultado<NivelAprobacionDto>.Exitoso(NivelAprobacionDto.Desde(nivel));
        });
    }

    /// <inheritdoc />
    public Task<Resultado> EliminarAsync(Guid id, CancellationToken cancelacion = default) =>
        ProtectorCasoUso.ProtegerAsync(async () =>
        {
            NivelAprobacion? nivel = await _repositorio.ObtenerPorIdAsync(id, cancelacion);

            if (nivel is null)
            {
                return Resultado.Fallido(ErrorApp.NoEncontrado("El nivel de aprobación solicitado no existe."));
            }

            _repositorio.Eliminar(nivel);
            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);
            return Resultado.Exitoso();
        });

    /// <inheritdoc />
    public async Task<string?> ObtenerAprobadorAsync(decimal montoCrc, CancellationToken cancelacion = default)
    {
        IReadOnlyList<NivelAprobacion> niveles = await _repositorio.ListarTodosAsync(cancelacion);
        return SelectorNivelAprobacion.Seleccionar(niveles, montoCrc)?.Aprobador;
    }
}
