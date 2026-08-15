using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Licitaciones.IntegrationTests.Comun;

/// <summary>
/// Base de las pruebas que ejercitan la API REST sobre la aplicación real.
/// </summary>
public abstract class PruebaApi : PruebaConBaseDatos, IDisposable
{
    private FabricaAplicacion? _fabrica;
    private HttpClient? _cliente;
    private bool _liberado;

    /// <summary>
    /// Prepara la prueba.
    /// </summary>
    /// <param name="contenedor">Contenedor compartido de PostgreSQL.</param>
    protected PruebaApi(ContenedorPostgres contenedor)
        : base(contenedor)
    {
    }

    /// <summary>Opciones de serialización equivalentes a las que expone la API.</summary>
    protected static JsonSerializerOptions OpcionesJson { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Cliente HTTP conectado a la aplicación bajo prueba.</summary>
    protected HttpClient Cliente => _cliente
        ?? throw new InvalidOperationException("La prueba todavía no fue inicializada.");

    /// <inheritdoc />
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _fabrica = new FabricaAplicacion(CadenaConexion, Reloj);
        _cliente = _fabrica.CreateClient();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Liberar(liberando: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Libera el cliente HTTP y la aplicación levantada por la prueba.
    /// </summary>
    /// <param name="liberando">Indica si la liberación es explícita.</param>
    protected virtual void Liberar(bool liberando)
    {
        if (_liberado)
        {
            return;
        }

        if (liberando)
        {
            _cliente?.Dispose();
            _fabrica?.Dispose();
        }

        _liberado = true;
    }

    /// <summary>
    /// Lee el cuerpo de una respuesta como JSON.
    /// </summary>
    /// <typeparam name="T">Tipo esperado.</typeparam>
    /// <param name="respuesta">Respuesta HTTP.</param>
    /// <returns>El cuerpo deserializado.</returns>
    protected static async Task<T> LeerAsync<T>(HttpResponseMessage respuesta)
    {
        ArgumentNullException.ThrowIfNull(respuesta);

        T? valor = await respuesta.Content.ReadFromJsonAsync<T>(OpcionesJson);
        Assert.NotNull(valor);
        return valor;
    }

    /// <summary>
    /// Lee el cuerpo de una respuesta de error como documento JSON.
    /// </summary>
    /// <param name="respuesta">Respuesta HTTP.</param>
    /// <returns>Documento con el ProblemDetails devuelto.</returns>
    protected static async Task<JsonDocument> LeerProblemaAsync(HttpResponseMessage respuesta)
    {
        ArgumentNullException.ThrowIfNull(respuesta);

        string cuerpo = await respuesta.Content.ReadAsStringAsync();
        return JsonDocument.Parse(cuerpo);
    }
}
