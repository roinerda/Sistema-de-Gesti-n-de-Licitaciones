using Licitaciones.Infrastructure;
using Licitaciones.Infrastructure.Persistencia;
using Licitaciones.Infrastructure.Reloj;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Testcontainers.PostgreSql;

namespace Licitaciones.FunctionalTests.Comun;

/// <summary>
/// Entorno completo para las pruebas de navegador: PostgreSQL, la aplicación sobre Kestrel y un
/// navegador de Playwright, compartidos por toda la serie.
/// </summary>
public sealed class ServidorE2E : IAsyncLifetime, IDisposable
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("licitaciones")
        .WithUsername("licitaciones")
        .WithPassword("clave-solo-de-pruebas")
        .Build();

    private FabricaServidorE2E? _fabrica;
    private IPlaywright? _playwright;
    private IBrowser? _navegador;

    /// <summary>Dirección base de la aplicación bajo prueba.</summary>
    public string DireccionBase { get; private set; } = string.Empty;

    /// <summary>Navegador compartido; cada prueba abre su propio contexto aislado.</summary>
    public IBrowser Navegador => _navegador
        ?? throw new InvalidOperationException("El navegador todavía no fue inicializado.");

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        string cadenaConexion = _postgres.GetConnectionString();

        // La aplicación lee la cadena de conexión del entorno, igual que en Docker y en Kubernetes.
        Environment.SetEnvironmentVariable("ConnectionStrings__Licitaciones", cadenaConexion);
        Environment.SetEnvironmentVariable("BaseDatos__AplicarMigracionesAlIniciar", "false");

        await MigrarAsync(cadenaConexion);

        _fabrica = new FabricaServidorE2E();
        _fabrica.CreateDefaultClient().Dispose();
        DireccionBase = _fabrica.DireccionBase;

        _playwright = await Playwright.CreateAsync();
        _navegador = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_navegador is not null)
        {
            await _navegador.CloseAsync();
        }

        Dispose();

        await _postgres.DisposeAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _playwright?.Dispose();
        _playwright = null;

        _fabrica?.Dispose();
        _fabrica = null;
    }

    /// <summary>
    /// Abre una página nueva en un contexto aislado, ya situada en la dirección indicada.
    /// </summary>
    /// <param name="ruta">Ruta relativa dentro de la aplicación.</param>
    /// <returns>Contexto y página listos para usar.</returns>
    public async Task<(IBrowserContext Contexto, IPage Pagina)> AbrirAsync(string ruta = "/")
    {
        IBrowserContext contexto = await Navegador.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = DireccionBase,
            Locale = "es-CR",
            TimezoneId = "America/Costa_Rica",
        });

        IPage pagina = await contexto.NewPageAsync();
        await pagina.GotoAsync(ruta);
        return (contexto, pagina);
    }

    private static async Task MigrarAsync(string cadenaConexion)
    {
        DbContextOptions<LicitacionesDbContext> opciones =
            new DbContextOptionsBuilder<LicitacionesDbContext>()
                .UseNpgsql(
                    cadenaConexion,
                    npgsql => npgsql.MigrationsHistoryTable(
                        ConfiguracionInfraestructura.TablaHistorialMigraciones))
                .Options;

        await using var contexto = new LicitacionesDbContext(opciones, new RelojSistema());
        await contexto.Database.MigrateAsync();
    }
}

/// <summary>
/// Serie de pruebas que comparten el entorno de extremo a extremo.
/// </summary>
[CollectionDefinition(Nombre)]
public sealed class ColeccionE2E : ICollectionFixture<ServidorE2E>
{
    /// <summary>Nombre de la serie.</summary>
    public const string Nombre = "e2e";
}
