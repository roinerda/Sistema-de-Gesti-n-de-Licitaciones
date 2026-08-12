using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Enumeraciones;
using Licitaciones.Domain.Normalizacion;
using Licitaciones.Domain.Servicios;

namespace Licitaciones.Application.Servicios;

/// <summary>
/// Implementación de los casos de uso de licitaciones.
/// </summary>
public sealed class ServicioLicitaciones : IServicioLicitaciones
{
    private readonly IRepositorioLicitaciones _repositorio;
    private readonly IRepositorioOfertas _repositorioOfertas;
    private readonly IServicioNivelesAprobacion _nivelesAprobacion;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>
    /// Crea el servicio con sus dependencias.
    /// </summary>
    /// <param name="repositorio">Puerto de acceso a licitaciones.</param>
    /// <param name="repositorioOfertas">Puerto de acceso a ofertas.</param>
    /// <param name="nivelesAprobacion">Servicio que resuelve el aprobador según el monto.</param>
    /// <param name="unidadDeTrabajo">Puerto de persistencia transaccional.</param>
    /// <param name="reloj">Reloj inyectable.</param>
    public ServicioLicitaciones(
        IRepositorioLicitaciones repositorio,
        IRepositorioOfertas repositorioOfertas,
        IServicioNivelesAprobacion nivelesAprobacion,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _repositorio = repositorio;
        _repositorioOfertas = repositorioOfertas;
        _nivelesAprobacion = nivelesAprobacion;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<PaginaResultado<LicitacionDto>> ListarAsync(
        ParametrosConsultaLicitaciones parametros,
        CancellationToken cancelacion = default)
    {
        PaginaResultado<Licitacion> pagina = await _repositorio.ListarAsync(parametros, cancelacion);
        Guid[] ids = pagina.Elementos.Select(l => l.Id).ToArray();
        IReadOnlyDictionary<Guid, int> conteos = await _repositorio.ContarOfertasAsync(ids, cancelacion);
        DateTimeOffset ahora = _reloj.Ahora;

        return pagina.Proyectar(l => LicitacionDto.Desde(l, ahora, conteos.TryGetValue(l.Id, out int total) ? total : 0));
    }

    /// <inheritdoc />
    public async Task<Resultado<LicitacionDetalleDto>> ObtenerAsync(Guid id, CancellationToken cancelacion = default)
    {
        Licitacion? licitacion = await _repositorio.ObtenerPorIdAsync(id, incluirEliminadas: true, cancelacion);

        if (licitacion is null)
        {
            return Resultado<LicitacionDetalleDto>.Fallido(ErrorApp.NoEncontrado("La licitación solicitada no existe."));
        }

        IReadOnlyList<Oferta> ofertas = await _repositorioOfertas.ListarPorLicitacionAsync(id, cancelacion);
        MejorOfertaDto mejorOferta = await EvaluarAsync(licitacion, ofertas, cancelacion);

        var detalle = new LicitacionDetalleDto(
            LicitacionDto.Desde(licitacion, _reloj.Ahora, ofertas.Count),
            mejorOferta,
            TransicionesLicitacion.DestinosDesde(licitacion.Estado));

        return Resultado<LicitacionDetalleDto>.Exitoso(detalle);
    }

    /// <inheritdoc />
    public Task<Resultado<LicitacionDto>> CrearAsync(GuardarLicitacionDto datos, CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(datos);

        return ProtectorCasoUso.ProtegerAsync(
            async () =>
            {
                var licitacion = Licitacion.Crear(
                    datos.Codigo,
                    datos.Titulo,
                    datos.FechaCierre,
                    datos.PresupuestoEstimadoCrc,
                    _reloj.Ahora);

                if (await _repositorio.ExisteCodigoAsync(licitacion.CodigoNormalizado, null, cancelacion))
                {
                    return Resultado<LicitacionDto>.Fallido(ErrorCodigoDuplicado(licitacion.Codigo));
                }

                _repositorio.Agregar(licitacion);
                await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);
                return Resultado<LicitacionDto>.Exitoso(LicitacionDto.Desde(licitacion, _reloj.Ahora));
            },
            _ => ErrorCodigoDuplicado(datos.Codigo));
    }

    /// <inheritdoc />
    public Task<Resultado<LicitacionDto>> ActualizarAsync(
        Guid id,
        GuardarLicitacionDto datos,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(datos);

        return ProtectorCasoUso.ProtegerAsync(
            async () =>
            {
                Licitacion? licitacion = await _repositorio.ObtenerPorIdAsync(id, incluirEliminadas: false, cancelacion);

                if (licitacion is null)
                {
                    return Resultado<LicitacionDto>.Fallido(ErrorApp.NoEncontrado("La licitación solicitada no existe."));
                }

                string codigoNormalizado = NormalizadorTexto.NormalizarCodigo(datos.Codigo);

                if (await _repositorio.ExisteCodigoAsync(codigoNormalizado, id, cancelacion))
                {
                    return Resultado<LicitacionDto>.Fallido(ErrorCodigoDuplicado(datos.Codigo));
                }

                if (datos.Version is { } version)
                {
                    _unidadDeTrabajo.EstablecerVersionOriginal(licitacion, version);
                }

                // El presupuesto no puede quedar por debajo de una oferta ya registrada (sección 8.5).
                decimal? montoMayor = await _repositorio.ObtenerMontoOfertaMayorAsync(id, cancelacion);

                licitacion.ActualizarDatos(
                    datos.Codigo,
                    datos.Titulo,
                    datos.FechaCierre,
                    datos.PresupuestoEstimadoCrc,
                    montoMayor,
                    _reloj.Ahora);

                await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

                int cantidadOfertas = await _repositorio.ContarOfertasAsync(id, cancelacion);
                return Resultado<LicitacionDto>.Exitoso(LicitacionDto.Desde(licitacion, _reloj.Ahora, cantidadOfertas));
            },
            _ => ErrorCodigoDuplicado(datos.Codigo));
    }

    /// <inheritdoc />
    public Task<Resultado<LicitacionDto>> CambiarEstadoAsync(
        Guid id,
        CambiarEstadoLicitacionDto datos,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(datos);

        return ProtectorCasoUso.ProtegerAsync(async () =>
        {
            Licitacion? licitacion = await _repositorio.ObtenerPorIdAsync(id, incluirEliminadas: false, cancelacion);

            if (licitacion is null)
            {
                return Resultado<LicitacionDto>.Fallido(ErrorApp.NoEncontrado("La licitación solicitada no existe."));
            }

            licitacion.CambiarEstado(datos.NuevoEstado, _reloj.Ahora);
            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

            int cantidadOfertas = await _repositorio.ContarOfertasAsync(id, cancelacion);
            return Resultado<LicitacionDto>.Exitoso(LicitacionDto.Desde(licitacion, _reloj.Ahora, cantidadOfertas));
        });
    }

    /// <inheritdoc />
    public Task<Resultado> EliminarAsync(Guid id, CancellationToken cancelacion = default) =>
        ProtectorCasoUso.ProtegerAsync(async () =>
        {
            Licitacion? licitacion = await _repositorio.ObtenerPorIdAsync(id, incluirEliminadas: true, cancelacion);

            if (licitacion is null)
            {
                return Resultado.Fallido(ErrorApp.NoEncontrado("La licitación solicitada no existe."));
            }

            // Borrado lógico consistente: las ofertas asociadas se conservan como evidencia (sección 8.9).
            licitacion.Eliminar(_reloj.Ahora);
            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);
            return Resultado.Exitoso();
        });

    /// <inheritdoc />
    public async Task<Resultado<MejorOfertaDto>> ObtenerMejorOfertaAsync(Guid id, CancellationToken cancelacion = default)
    {
        Licitacion? licitacion = await _repositorio.ObtenerPorIdAsync(id, incluirEliminadas: true, cancelacion);

        if (licitacion is null)
        {
            return Resultado<MejorOfertaDto>.Fallido(ErrorApp.NoEncontrado("La licitación solicitada no existe."));
        }

        IReadOnlyList<Oferta> ofertas = await _repositorioOfertas.ListarPorLicitacionAsync(id, cancelacion);
        return Resultado<MejorOfertaDto>.Exitoso(await EvaluarAsync(licitacion, ofertas, cancelacion));
    }

    private async Task<MejorOfertaDto> EvaluarAsync(
        Licitacion licitacion,
        IReadOnlyList<Oferta> ofertas,
        CancellationToken cancelacion)
    {
        EvaluacionOfertas evaluacion = EvaluadorOfertas.Evaluar(licitacion.PresupuestoEstimadoCrc, ofertas);

        string? aprobador = evaluacion.MejorOferta is null
            ? null
            : await _nivelesAprobacion.ObtenerAprobadorAsync(evaluacion.MejorOferta.MontoOfertadoCrc, cancelacion);

        return new MejorOfertaDto(
            licitacion.Id,
            licitacion.Codigo,
            licitacion.PresupuestoEstimadoCrc,
            evaluacion.MejorOferta is null ? null : OfertaDto.Desde(evaluacion.MejorOferta),
            evaluacion.PorcentajeAhorro,
            evaluacion.Clasificacion,
            evaluacion.Clasificacion.Descripcion(),
            aprobador);
    }

    private static ErrorApp ErrorCodigoDuplicado(string codigo) =>
        ErrorApp.Conflicto(
            CodigosError.CodigoLicitacionDuplicado,
            $"Ya existe una licitación con el código «{NormalizadorTexto.LimpiarEspacios(codigo)}».",
            nameof(GuardarLicitacionDto.Codigo));
}
