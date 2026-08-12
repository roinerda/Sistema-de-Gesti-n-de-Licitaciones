using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Entidades;

namespace Licitaciones.Application.Servicios;

/// <summary>
/// Implementación de los casos de uso de ofertas.
/// </summary>
/// <remarks>
/// Todas las operaciones exigen que la licitación esté publicada y vigente: una oferta vencida o
/// perteneciente a una licitación cerrada no puede crearse, editarse ni eliminarse (sección 8.2).
/// </remarks>
public sealed class ServicioOfertas : IServicioOfertas
{
    private readonly IRepositorioOfertas _repositorio;
    private readonly IRepositorioLicitaciones _repositorioLicitaciones;
    private readonly IRepositorioProveedores _repositorioProveedores;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>
    /// Crea el servicio con sus dependencias.
    /// </summary>
    /// <param name="repositorio">Puerto de acceso a ofertas.</param>
    /// <param name="repositorioLicitaciones">Puerto de acceso a licitaciones.</param>
    /// <param name="repositorioProveedores">Puerto de acceso a proveedores.</param>
    /// <param name="unidadDeTrabajo">Puerto de persistencia transaccional.</param>
    /// <param name="reloj">Reloj inyectable.</param>
    public ServicioOfertas(
        IRepositorioOfertas repositorio,
        IRepositorioLicitaciones repositorioLicitaciones,
        IRepositorioProveedores repositorioProveedores,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _repositorio = repositorio;
        _repositorioLicitaciones = repositorioLicitaciones;
        _repositorioProveedores = repositorioProveedores;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<PaginaResultado<OfertaDto>> ListarAsync(
        ParametrosConsultaOfertas parametros,
        CancellationToken cancelacion = default)
    {
        PaginaResultado<Oferta> pagina = await _repositorio.ListarAsync(parametros, cancelacion);
        return pagina.Proyectar(OfertaDto.Desde);
    }

    /// <inheritdoc />
    public async Task<Resultado<OfertaDto>> ObtenerAsync(Guid id, CancellationToken cancelacion = default)
    {
        Oferta? oferta = await _repositorio.ObtenerPorIdAsync(id, cancelacion);

        return oferta is null
            ? Resultado<OfertaDto>.Fallido(ErrorApp.NoEncontrado("La oferta solicitada no existe."))
            : Resultado<OfertaDto>.Exitoso(OfertaDto.Desde(oferta));
    }

    /// <inheritdoc />
    public Task<Resultado<OfertaDto>> CrearAsync(GuardarOfertaDto datos, CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(datos);

        return ProtectorCasoUso.ProtegerAsync(
            async () =>
            {
                Licitacion? licitacion = await _repositorioLicitaciones.ObtenerPorIdAsync(
                    datos.LicitacionId,
                    incluirEliminadas: false,
                    cancelacion);

                if (licitacion is null)
                {
                    return Resultado<OfertaDto>.Fallido(ErrorApp.NoEncontrado("La licitación indicada no existe."));
                }

                Proveedor? proveedor = await _repositorioProveedores.ObtenerPorIdAsync(
                    datos.ProveedorId,
                    incluirEliminados: false,
                    cancelacion);

                if (proveedor is null)
                {
                    return Resultado<OfertaDto>.Fallido(ErrorApp.NoEncontrado("El proveedor indicado no existe."));
                }

                bool duplicada = await _repositorio.ExisteOfertaDeProveedorAsync(
                    datos.LicitacionId,
                    datos.ProveedorId,
                    null,
                    cancelacion);

                if (duplicada)
                {
                    return Resultado<OfertaDto>.Fallido(ErrorOfertaDuplicada(proveedor.Nombre));
                }

                var oferta = Oferta.Crear(licitacion, proveedor, datos.MontoOfertadoCrc, _reloj.Ahora);
                _repositorio.Agregar(oferta);
                await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

                Oferta? persistida = await _repositorio.ObtenerPorIdAsync(oferta.Id, cancelacion);
                return Resultado<OfertaDto>.Exitoso(OfertaDto.Desde(persistida ?? oferta));
            },
            _ => ErrorOfertaDuplicada(null));
    }

    /// <inheritdoc />
    public Task<Resultado<OfertaDto>> ActualizarAsync(
        Guid id,
        ActualizarOfertaDto datos,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(datos);

        return ProtectorCasoUso.ProtegerAsync(async () =>
        {
            Oferta? oferta = await _repositorio.ObtenerPorIdAsync(id, cancelacion);

            if (oferta is null)
            {
                return Resultado<OfertaDto>.Fallido(ErrorApp.NoEncontrado("La oferta solicitada no existe."));
            }

            Licitacion? licitacion = oferta.Licitacion
                ?? await _repositorioLicitaciones.ObtenerPorIdAsync(oferta.LicitacionId, incluirEliminadas: true, cancelacion);

            if (licitacion is null)
            {
                return Resultado<OfertaDto>.Fallido(ErrorApp.NoEncontrado("La licitación de la oferta no existe."));
            }

            if (datos.Version is { } version)
            {
                _unidadDeTrabajo.EstablecerVersionOriginal(oferta, version);
            }

            oferta.ActualizarMonto(licitacion, datos.MontoOfertadoCrc, _reloj.Ahora);
            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);
            return Resultado<OfertaDto>.Exitoso(OfertaDto.Desde(oferta));
        });
    }

    /// <inheritdoc />
    public Task<Resultado> EliminarAsync(Guid id, CancellationToken cancelacion = default) =>
        ProtectorCasoUso.ProtegerAsync(async () =>
        {
            Oferta? oferta = await _repositorio.ObtenerPorIdAsync(id, cancelacion);

            if (oferta is null)
            {
                return Resultado.Fallido(ErrorApp.NoEncontrado("La oferta solicitada no existe."));
            }

            Licitacion? licitacion = oferta.Licitacion
                ?? await _repositorioLicitaciones.ObtenerPorIdAsync(oferta.LicitacionId, incluirEliminadas: true, cancelacion);

            if (licitacion is null)
            {
                return Resultado.Fallido(ErrorApp.NoEncontrado("La licitación de la oferta no existe."));
            }

            // Las ofertas de licitaciones cerradas o vencidas se conservan como evidencia.
            licitacion.GarantizarQueAceptaOfertas(_reloj.Ahora);

            _repositorio.Eliminar(oferta);
            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);
            return Resultado.Exitoso();
        });

    private static ErrorApp ErrorOfertaDuplicada(string? nombreProveedor) =>
        ErrorApp.Conflicto(
            CodigosError.OfertaDuplicada,
            nombreProveedor is null
                ? "El proveedor ya tiene una oferta registrada en esta licitación."
                : $"El proveedor «{nombreProveedor}» ya tiene una oferta registrada en esta licitación.",
            nameof(GuardarOfertaDto.ProveedorId));
}
