using System.Globalization;
using Licitaciones.Api;
using Licitaciones.Application;
using Licitaciones.Infrastructure;
using Licitaciones.Infrastructure.Persistencia;
using Licitaciones.Web.Filtros;
using Microsoft.AspNetCore.Localization;

WebApplicationBuilder constructor = WebApplication.CreateBuilder(args);

// Capas de la solución. El orden refleja la dirección de las dependencias:
// presentación -> aplicación -> dominio, con la infraestructura implementando los puertos.
constructor.Services.AgregarCapaAplicacion();
constructor.Services.AgregarCapaInfraestructura(constructor.Configuration);
constructor.Services.AgregarApiRest();

constructor.Services.AddScoped<FiltroTipoCambioActivo>();
constructor.Services.AddControllersWithViews(opciones =>
{
    // Todas las vistas necesitan el tipo de cambio activo para poder alternar entre CRC y USD.
    opciones.Filters.AddService<FiltroTipoCambioActivo>();
});

constructor.Services.AddHealthChecks();

WebApplication aplicacion = constructor.Build();

// La cultura es-CR gobierna el formato de montos y fechas en toda la interfaz.
var culturaCostaRica = new CultureInfo("es-CR");
aplicacion.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culturaCostaRica),
    SupportedCultures = [culturaCostaRica],
    SupportedUICultures = [culturaCostaRica],
});

if (!aplicacion.Environment.IsDevelopment())
{
    aplicacion.UseExceptionHandler("/Inicio/Error");
    aplicacion.UseHsts();
}

aplicacion.UseStaticFiles();
aplicacion.UseRouting();

aplicacion.MapControllerRoute(
    name: "default",
    pattern: "{controller=Inicio}/{action=Index}/{id?}");

aplicacion.UsarApiRest();

// Sondas separadas: «vivo» solo confirma que el proceso responde y «listo» además comprueba
// la base de datos. Kubernetes necesita distinguirlas para no reiniciar un pod sano que
// simplemente está esperando a PostgreSQL.
aplicacion.MapHealthChecks("/salud/vivo", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false,
});
aplicacion.MapHealthChecks("/salud/listo");

await AplicarMigracionesSiCorrespondeAsync(aplicacion);

await aplicacion.RunAsync();

// Aplica las migraciones al iniciar cuando la configuración lo autoriza.
static async Task AplicarMigracionesSiCorrespondeAsync(WebApplication aplicacion)
{
    bool aplicar = aplicacion.Configuration.GetValue("BaseDatos:AplicarMigracionesAlIniciar", defaultValue: true);

    if (!aplicar)
    {
        return;
    }

    using IServiceScope ambito = aplicacion.Services.CreateScope();
    InicializadorBaseDatos inicializador = ambito.ServiceProvider.GetRequiredService<InicializadorBaseDatos>();
    await inicializador.MigrarAsync();
}

/// <summary>
/// Punto de entrada expuesto para que las pruebas funcionales y de integración puedan
/// levantar la aplicación con <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program
{
    /// <summary>Constructor protegido: la clase solo existe como marcador de tipo.</summary>
    protected Program()
    {
    }
}
