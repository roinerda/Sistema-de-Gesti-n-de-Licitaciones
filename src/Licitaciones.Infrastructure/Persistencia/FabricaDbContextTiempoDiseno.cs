using Licitaciones.Domain.Comun;
using Licitaciones.Infrastructure.Reloj;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Licitaciones.Infrastructure.Persistencia;

/// <summary>
/// Fábrica del contexto usada solo por las herramientas de línea de comandos de Entity Framework Core.
/// </summary>
/// <remarks>
/// Permite ejecutar <c>dotnet ef migrations add</c> sin levantar la aplicación web. La cadena de conexión
/// se toma de la variable de entorno <c>ConnectionStrings__Licitaciones</c>; si no está definida se usa una
/// cadena local de desarrollo, ya que generar una migración no requiere conectarse a la base de datos.
/// </remarks>
public sealed class FabricaDbContextTiempoDiseno : IDesignTimeDbContextFactory<LicitacionesDbContext>
{
    private const string CadenaDesarrollo =
        "Host=localhost;Port=5432;Database=licitaciones;Username=licitaciones;Password=cambiar_en_local";

    /// <inheritdoc />
    public LicitacionesDbContext CreateDbContext(string[] args)
    {
        string cadena = Environment.GetEnvironmentVariable("ConnectionStrings__Licitaciones") ?? CadenaDesarrollo;

        DbContextOptions<LicitacionesDbContext> opciones = new DbContextOptionsBuilder<LicitacionesDbContext>()
            .UseNpgsql(cadena, npgsql => npgsql.MigrationsHistoryTable(ConfiguracionInfraestructura.TablaHistorialMigraciones))
            .Options;

        IReloj reloj = new RelojSistema();
        return new LicitacionesDbContext(opciones, reloj);
    }
}
