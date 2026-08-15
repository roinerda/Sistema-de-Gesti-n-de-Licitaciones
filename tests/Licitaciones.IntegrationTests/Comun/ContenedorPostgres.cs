using Npgsql;
using Testcontainers.PostgreSql;

namespace Licitaciones.IntegrationTests.Comun;

/// <summary>
/// Contenedor de PostgreSQL compartido por toda la serie de pruebas de integración.
/// </summary>
/// <remarks>
/// Levantar el motor una sola vez y crear una base de datos por clase de prueba mantiene el
/// aislamiento sin pagar el arranque del contenedor en cada clase. La versión coincide con la
/// que se despliega en Docker Compose y en Kubernetes, de modo que lo que se prueba es lo que
/// se ejecuta en producción.
/// </remarks>
public sealed class ContenedorPostgres : IAsyncLifetime
{
    /// <summary>Imagen del motor. Debe coincidir con la de docker-compose.yml y la del StatefulSet.</summary>
    public const string Imagen = "postgres:16-alpine";

    private readonly PostgreSqlContainer _contenedor = new PostgreSqlBuilder(Imagen)
        .WithDatabase("licitaciones")
        .WithUsername("licitaciones")
        .WithPassword("clave-solo-de-pruebas")
        .Build();

    /// <summary>Cadena de conexión a la base de datos administrativa del contenedor.</summary>
    public string CadenaConexionAdministracion => _contenedor.GetConnectionString();

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _contenedor.StartAsync();

        // El host lee la cadena de conexión mientras se construye, antes de que las pruebas puedan
        // intervenir el contenedor de servicios. Se deja una cadena válida en el entorno para que
        // el arranque no falle; cada prueba de API sustituye después el contexto por el de su
        // propia base de datos. Las migraciones al iniciar se desactivan porque las aplica la prueba.
        Environment.SetEnvironmentVariable("ConnectionStrings__Licitaciones", CadenaConexionAdministracion);
        Environment.SetEnvironmentVariable("BaseDatos__AplicarMigracionesAlIniciar", "false");
    }

    /// <inheritdoc />
    public async Task DisposeAsync() => await _contenedor.DisposeAsync();

    /// <summary>
    /// Crea una base de datos vacía y devuelve su cadena de conexión.
    /// </summary>
    /// <param name="nombre">Nombre de la base de datos a crear.</param>
    /// <returns>Cadena de conexión apuntando a la base recién creada.</returns>
    public async Task<string> CrearBaseDatosAsync(string nombre)
    {
        await EjecutarAsync($"CREATE DATABASE \"{nombre}\"");

        var constructor = new NpgsqlConnectionStringBuilder(CadenaConexionAdministracion)
        {
            Database = nombre,
        };

        return constructor.ConnectionString;
    }

    /// <summary>
    /// Elimina una base de datos creada por las pruebas.
    /// </summary>
    /// <param name="nombre">Nombre de la base de datos a eliminar.</param>
    /// <returns>Tarea que finaliza cuando la base fue eliminada.</returns>
    public Task EliminarBaseDatosAsync(string nombre) =>
        EjecutarAsync($"DROP DATABASE IF EXISTS \"{nombre}\" WITH (FORCE)");

    private async Task EjecutarAsync(string sentencia)
    {
        await using var conexion = new NpgsqlConnection(CadenaConexionAdministracion);
        await conexion.OpenAsync();

        await using NpgsqlCommand comando = conexion.CreateCommand();
        comando.CommandText = sentencia;
        await comando.ExecuteNonQueryAsync();
    }
}

/// <summary>
/// Serie de pruebas que comparten el contenedor de PostgreSQL.
/// </summary>
[CollectionDefinition(Nombre)]
public sealed class ColeccionPostgres : ICollectionFixture<ContenedorPostgres>
{
    /// <summary>Nombre de la serie, referenciado en cada clase de prueba.</summary>
    public const string Nombre = "postgres";
}
