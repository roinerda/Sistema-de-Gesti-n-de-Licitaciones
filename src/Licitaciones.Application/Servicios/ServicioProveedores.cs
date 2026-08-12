using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Normalizacion;

namespace Licitaciones.Application.Servicios;

/// <summary>
/// Implementación de los casos de uso de proveedores.
/// </summary>
/// <remarks>
/// La unicidad del nombre se comprueba aquí para dar un mensaje claro, y además está respaldada por un
/// índice único en PostgreSQL que actúa como última línea de defensa ante peticiones simultáneas.
/// </remarks>
public sealed class ServicioProveedores : IServicioProveedores
{
    private readonly IRepositorioProveedores _repositorio;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>
    /// Crea el servicio con sus dependencias.
    /// </summary>
    /// <param name="repositorio">Puerto de acceso a proveedores.</param>
    /// <param name="unidadDeTrabajo">Puerto de persistencia transaccional.</param>
    /// <param name="reloj">Reloj inyectable.</param>
    public ServicioProveedores(IRepositorioProveedores repositorio, IUnidadDeTrabajo unidadDeTrabajo, IReloj reloj)
    {
        _repositorio = repositorio;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<PaginaResultado<ProveedorDto>> ListarAsync(
        ParametrosConsultaProveedores parametros,
        CancellationToken cancelacion = default)
    {
        PaginaResultado<Proveedor> pagina = await _repositorio.ListarAsync(parametros, cancelacion);
        Guid[] ids = pagina.Elementos.Select(p => p.Id).ToArray();

        // Una sola consulta agregada evita el problema N+1 al mostrar la cantidad de ofertas por fila.
        IReadOnlyDictionary<Guid, int> conteos = await _repositorio.ContarOfertasAsync(ids, cancelacion);

        return pagina.Proyectar(p => ProveedorDto.Desde(p, conteos.TryGetValue(p.Id, out int total) ? total : 0));
    }

    /// <inheritdoc />
    public async Task<Resultado<ProveedorDto>> ObtenerAsync(Guid id, CancellationToken cancelacion = default)
    {
        Proveedor? proveedor = await _repositorio.ObtenerPorIdAsync(id, incluirEliminados: true, cancelacion);

        if (proveedor is null)
        {
            return Resultado<ProveedorDto>.Fallido(ErrorApp.NoEncontrado("El proveedor solicitado no existe."));
        }

        int cantidadOfertas = await _repositorio.ContarOfertasAsync(id, cancelacion);
        return Resultado<ProveedorDto>.Exitoso(ProveedorDto.Desde(proveedor, cantidadOfertas));
    }

    /// <inheritdoc />
    public Task<Resultado<ProveedorDto>> CrearAsync(GuardarProveedorDto datos, CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(datos);

        return ProtectorCasoUso.ProtegerAsync(
            async () =>
            {
                var proveedor = Proveedor.Crear(datos.Nombre, _reloj.Ahora);

                if (await _repositorio.ExisteNombreAsync(proveedor.NombreNormalizado, null, cancelacion))
                {
                    return Resultado<ProveedorDto>.Fallido(ErrorNombreDuplicado(proveedor.Nombre));
                }

                _repositorio.Agregar(proveedor);
                await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);
                return Resultado<ProveedorDto>.Exitoso(ProveedorDto.Desde(proveedor));
            },
            _ => ErrorNombreDuplicado(datos.Nombre));
    }

    /// <inheritdoc />
    public Task<Resultado<ProveedorDto>> ActualizarAsync(
        Guid id,
        GuardarProveedorDto datos,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(datos);

        return ProtectorCasoUso.ProtegerAsync(
            async () =>
            {
                Proveedor? proveedor = await _repositorio.ObtenerPorIdAsync(id, incluirEliminados: false, cancelacion);

                if (proveedor is null)
                {
                    return Resultado<ProveedorDto>.Fallido(ErrorApp.NoEncontrado("El proveedor solicitado no existe."));
                }

                string nombreNormalizado = NormalizadorTexto.NormalizarNombre(datos.Nombre);

                if (await _repositorio.ExisteNombreAsync(nombreNormalizado, id, cancelacion))
                {
                    return Resultado<ProveedorDto>.Fallido(ErrorNombreDuplicado(datos.Nombre));
                }

                if (datos.Version is { } version)
                {
                    _unidadDeTrabajo.EstablecerVersionOriginal(proveedor, version);
                }

                proveedor.Renombrar(datos.Nombre, _reloj.Ahora);
                await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

                int cantidadOfertas = await _repositorio.ContarOfertasAsync(id, cancelacion);
                return Resultado<ProveedorDto>.Exitoso(ProveedorDto.Desde(proveedor, cantidadOfertas));
            },
            _ => ErrorNombreDuplicado(datos.Nombre));
    }

    /// <inheritdoc />
    public Task<Resultado> EliminarAsync(Guid id, CancellationToken cancelacion = default) =>
        ProtectorCasoUso.ProtegerAsync(async () =>
        {
            Proveedor? proveedor = await _repositorio.ObtenerPorIdAsync(id, incluirEliminados: true, cancelacion);

            if (proveedor is null)
            {
                return Resultado.Fallido(ErrorApp.NoEncontrado("El proveedor solicitado no existe."));
            }

            // Siempre se aplica borrado lógico: conserva las ofertas como evidencia y mantiene
            // la integridad referencial sin depender de que el proveedor tenga o no ofertas.
            proveedor.Eliminar(_reloj.Ahora);
            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);
            return Resultado.Exitoso();
        });

    private static ErrorApp ErrorNombreDuplicado(string nombre) =>
        ErrorApp.Conflicto(
            CodigosError.NombreProveedorDuplicado,
            $"Ya existe un proveedor registrado con el nombre «{NormalizadorTexto.LimpiarEspacios(nombre)}».",
            nameof(GuardarProveedorDto.Nombre));
}
