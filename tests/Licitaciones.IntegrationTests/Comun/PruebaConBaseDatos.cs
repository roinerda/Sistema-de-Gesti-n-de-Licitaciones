using Licitaciones.Application.Abstracciones;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Enumeraciones;
using Licitaciones.Infrastructure;
using Licitaciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.IntegrationTests.Comun;

/// <summary>
/// Base de las pruebas que necesitan un esquema propio dentro del contenedor compartido.
/// </summary>
/// <remarks>
/// xUnit crea una instancia por cada prueba, de modo que cada prueba obtiene una base de datos
/// recién migrada y la elimina al terminar. Así ninguna depende del orden de ejecución ni arrastra
/// datos de otra, y los registros sembrados por las migraciones (estados, niveles y tipo de cambio
/// inicial) están siempre presentes tal como los encontraría un despliegue nuevo. El precio es
/// crear y migrar una base por prueba; a esta escala compensa frente a la fragilidad de compartir.
/// </remarks>
public abstract class PruebaConBaseDatos : IAsyncLifetime
{
    private readonly ContenedorPostgres _contenedor;
    private readonly string _nombreBaseDatos;

    /// <summary>
    /// Prepara la prueba reservando un nombre de base de datos irrepetible.
    /// </summary>
    /// <param name="contenedor">Contenedor compartido de PostgreSQL.</param>
    protected PruebaConBaseDatos(ContenedorPostgres contenedor)
    {
        _contenedor = contenedor;
        _nombreBaseDatos = $"prueba_{Guid.NewGuid():N}";
    }

    /// <summary>Cadena de conexión de la base de datos de esta clase de prueba.</summary>
    protected string CadenaConexion { get; private set; } = string.Empty;

    /// <summary>Reloj controlado por la prueba.</summary>
    protected RelojFijo Reloj { get; } = new();

    /// <inheritdoc />
    public virtual async Task InitializeAsync()
    {
        CadenaConexion = await _contenedor.CrearBaseDatosAsync(_nombreBaseDatos);

        await using LicitacionesDbContext contexto = CrearContexto();
        await contexto.Database.MigrateAsync();
    }

    /// <inheritdoc />
    public virtual Task DisposeAsync() => _contenedor.EliminarBaseDatosAsync(_nombreBaseDatos);

    /// <summary>
    /// Crea un contexto nuevo apuntando a la base de datos de la prueba.
    /// </summary>
    /// <returns>Contexto sin seguimiento previo, equivalente al de una petición nueva.</returns>
    protected LicitacionesDbContext CrearContexto()
    {
        DbContextOptions<LicitacionesDbContext> opciones =
            new DbContextOptionsBuilder<LicitacionesDbContext>()
                .UseNpgsql(
                    CadenaConexion,
                    npgsql => npgsql.MigrationsHistoryTable(ConfiguracionInfraestructura.TablaHistorialMigraciones))
                .Options;

        return new LicitacionesDbContext(opciones, Reloj);
    }

    /// <summary>
    /// Ejecuta una operación con un contexto propio, imitando el alcance de una petición HTTP.
    /// </summary>
    /// <typeparam name="T">Tipo del valor devuelto.</typeparam>
    /// <param name="operacion">Operación a ejecutar.</param>
    /// <returns>El valor devuelto por la operación.</returns>
    protected async Task<T> EnContextoAsync<T>(Func<LicitacionesDbContext, IUnidadDeTrabajo, Task<T>> operacion)
    {
        ArgumentNullException.ThrowIfNull(operacion);

        await using LicitacionesDbContext contexto = CrearContexto();
        return await operacion(contexto, new UnidadDeTrabajo(contexto));
    }

    /// <summary>Inserta un proveedor y devuelve su identificador.</summary>
    /// <param name="nombre">Nombre visible del proveedor.</param>
    /// <returns>Identificador del proveedor insertado.</returns>
    protected async Task<Guid> SembrarProveedorAsync(string nombre)
    {
        await using LicitacionesDbContext contexto = CrearContexto();
        Proveedor proveedor = Proveedor.Crear(nombre, Reloj.Ahora);
        contexto.Proveedores.Add(proveedor);
        await contexto.SaveChangesAsync();
        return proveedor.Id;
    }

    /// <summary>Inserta una licitación publicada y devuelve su identificador.</summary>
    /// <param name="codigo">Código visible de la licitación.</param>
    /// <param name="presupuestoCrc">Presupuesto estimado en colones.</param>
    /// <param name="publicada">Indica si debe quedar publicada.</param>
    /// <returns>Identificador de la licitación insertada.</returns>
    protected async Task<Guid> SembrarLicitacionAsync(
        string codigo,
        decimal presupuestoCrc = 5_000_000m,
        bool publicada = true)
    {
        await using LicitacionesDbContext contexto = CrearContexto();

        Licitacion licitacion = Licitacion.Crear(
            codigo,
            "Compra de equipo de cómputo",
            Reloj.Ahora.AddDays(30),
            presupuestoCrc,
            Reloj.Ahora);

        if (publicada)
        {
            licitacion.CambiarEstado(EstadoLicitacion.Publicada, Reloj.Ahora);
        }

        contexto.Licitaciones.Add(licitacion);
        await contexto.SaveChangesAsync();
        return licitacion.Id;
    }

    /// <summary>Inserta una oferta y devuelve su identificador.</summary>
    /// <param name="licitacionId">Licitación a la que se oferta.</param>
    /// <param name="proveedorId">Proveedor que oferta.</param>
    /// <param name="montoCrc">Monto ofertado en colones.</param>
    /// <returns>Identificador de la oferta insertada.</returns>
    protected async Task<Guid> SembrarOfertaAsync(Guid licitacionId, Guid proveedorId, decimal montoCrc)
    {
        await using LicitacionesDbContext contexto = CrearContexto();

        Licitacion licitacion = await contexto.Licitaciones.SingleAsync(l => l.Id == licitacionId);
        Proveedor proveedor = await contexto.Proveedores.SingleAsync(p => p.Id == proveedorId);

        Oferta oferta = Oferta.Crear(licitacion, proveedor, montoCrc, Reloj.Ahora);
        contexto.Ofertas.Add(oferta);
        await contexto.SaveChangesAsync();
        return oferta.Id;
    }
}
