using Licitaciones.Application.Abstracciones;
using Licitaciones.Domain.Entidades;
using Licitaciones.Infrastructure.Persistencia;
using Licitaciones.IntegrationTests.Comun;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.IntegrationTests.Persistencia;

/// <summary>
/// Comprueba en el motor real la unicidad normalizada del nombre y el borrado lógico de proveedores.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public sealed class PersistenciaProveedoresTests : PruebaConBaseDatos
{
    public PersistenciaProveedoresTests(ContenedorPostgres contenedor)
        : base(contenedor)
    {
    }

    [Theory]
    [InlineData("Constructora Alfa", "constructora alfa")]
    [InlineData("Constructora Alfa", "  Constructora   Alfa  ")]
    [InlineData("Constructora Alfa", "CONSTRUCTORA ALFA")]
    public async Task PostgreSQL_RechazaNombresQueSoloDifierenEnMayusculasOEspacios(
        string primero,
        string segundo)
    {
        await SembrarProveedorAsync(primero);

        // Se inserta sin pasar por el caso de uso: aquí se prueba la última línea de defensa,
        // el índice único de PostgreSQL, que es la que protege ante dos peticiones simultáneas.
        ViolacionUnicidadException excepcion = await Assert.ThrowsAsync<ViolacionUnicidadException>(
            () => InsertarProveedorAsync(segundo));

        Assert.Equal("ux_proveedores_nombre_normalizado", excepcion.NombreIndice);
        Assert.DoesNotContain("npgsql", excepcion.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BorradoLogico_LiberaElNombreParaUnProveedorNuevo()
    {
        Guid id = await SembrarProveedorAsync("Constructora Alfa");

        await using (LicitacionesDbContext contexto = CrearContexto())
        {
            Proveedor proveedor = await contexto.Proveedores.SingleAsync(p => p.Id == id);
            proveedor.Eliminar(Reloj.Ahora);
            await contexto.SaveChangesAsync();
        }

        // El índice único es parcial (deleted_at IS NULL), de modo que un proveedor dado de baja
        // no bloquea para siempre su nombre.
        Guid nuevoId = await SembrarProveedorAsync("Constructora Alfa");

        Assert.NotEqual(id, nuevoId);
    }

    [Fact]
    public async Task BorradoLogico_ConservaElRegistroYSuFechaDeBaja()
    {
        Guid id = await SembrarProveedorAsync("Constructora Alfa");

        await using (LicitacionesDbContext contexto = CrearContexto())
        {
            Proveedor proveedor = await contexto.Proveedores.SingleAsync(p => p.Id == id);
            proveedor.Eliminar(Reloj.Ahora);
            await contexto.SaveChangesAsync();
        }

        await using LicitacionesDbContext verificacion = CrearContexto();
        Proveedor eliminado = await verificacion.Proveedores.SingleAsync(p => p.Id == id);

        Assert.True(eliminado.EstaEliminado);
        Assert.Equal(Reloj.Ahora.ToUniversalTime(), eliminado.DeletedAt);
    }

    [Fact]
    public async Task NombreVacio_EsRechazadoPorLaRestriccionCheck()
    {
        Guid id = await SembrarProveedorAsync("Constructora Alfa");

        // Se escribe por SQL directo para evadir el dominio y comprobar que el motor también protege.
        Npgsql.PostgresException excepcion = await Assert.ThrowsAsync<Npgsql.PostgresException>(
            () => ConsultaSql.EjecutarAsync(
                CadenaConexion,
                "UPDATE proveedores SET nombre = '   ' WHERE id = $1",
                id));

        Assert.Equal("23514", excepcion.SqlState);
        Assert.Equal("ck_proveedores_nombre_no_vacio", excepcion.ConstraintName);
    }

    private async Task InsertarProveedorAsync(string nombre)
    {
        await using LicitacionesDbContext contexto = CrearContexto();
        var unidadDeTrabajo = new UnidadDeTrabajo(contexto);

        contexto.Proveedores.Add(Proveedor.Crear(nombre, Reloj.Ahora));
        await unidadDeTrabajo.GuardarCambiosAsync();
    }
}
