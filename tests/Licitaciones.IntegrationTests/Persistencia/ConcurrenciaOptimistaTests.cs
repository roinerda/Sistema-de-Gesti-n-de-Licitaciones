using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Licitaciones.Domain.Entidades;
using Licitaciones.Infrastructure.Persistencia;
using Licitaciones.Infrastructure.Persistencia.Repositorios;
using Licitaciones.IntegrationTests.Comun;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.IntegrationTests.Persistencia;

/// <summary>
/// Comprueba que la columna de versión detecte ediciones concurrentes contra PostgreSQL.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public sealed class ConcurrenciaOptimistaTests : PruebaConBaseDatos
{
    public ConcurrenciaOptimistaTests(ContenedorPostgres contenedor)
        : base(contenedor)
    {
    }

    [Fact]
    public async Task DosContextosQueEditanElMismoProveedor_ElSegundoRecibeConflicto()
    {
        Guid id = await SembrarProveedorAsync("Constructora Alfa");

        await using LicitacionesDbContext primero = CrearContexto();
        await using LicitacionesDbContext segundo = CrearContexto();

        Proveedor visionPrimero = await primero.Proveedores.SingleAsync(p => p.Id == id);
        Proveedor visionSegundo = await segundo.Proveedores.SingleAsync(p => p.Id == id);

        visionPrimero.Renombrar("Constructora Alfa S.A.", Reloj.Ahora);
        await new UnidadDeTrabajo(primero).GuardarCambiosAsync();

        visionSegundo.Renombrar("Constructora Alfa Limitada", Reloj.Ahora);

        await Assert.ThrowsAsync<ConflictoConcurrenciaException>(
            () => new UnidadDeTrabajo(segundo).GuardarCambiosAsync());
    }

    [Fact]
    public async Task GuardarConUnaVersionVieja_SeRechazaAunqueLaEdicionSeaPosterior()
    {
        Guid id = await SembrarProveedorAsync("Constructora Alfa");

        // Versión que la persona usuaria tenía a la vista al abrir el formulario.
        int versionDelFormulario = await ObtenerVersionAsync(id);

        // Otra persona guarda primero y la versión almacenada avanza.
        await ActualizarNombreAsync(id, "Constructora Alfa S.A.", versionDelFormulario);

        // La primera envía su formulario con la versión anterior: debe rechazarse.
        Resultado<ProveedorDto> resultado = await ActualizarNombreAsync(
            id,
            "Constructora Alfa Limitada",
            versionDelFormulario);

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.Concurrencia, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task CadaActualizacionExitosa_IncrementaLaVersionAlmacenada()
    {
        Guid id = await SembrarProveedorAsync("Constructora Alfa");
        int versionInicial = await ObtenerVersionAsync(id);

        Resultado<ProveedorDto> resultado = await ActualizarNombreAsync(
            id,
            "Constructora Alfa S.A.",
            versionInicial);

        Assert.True(resultado.EsExito);
        Assert.Equal(versionInicial + 1, await ObtenerVersionAsync(id));
    }

    private async Task<int> ObtenerVersionAsync(Guid id)
    {
        await using LicitacionesDbContext contexto = CrearContexto();
        return await contexto.Proveedores.Where(p => p.Id == id).Select(p => p.Version).SingleAsync();
    }

    private async Task<Resultado<ProveedorDto>> ActualizarNombreAsync(Guid id, string nombre, int version)
    {
        await using LicitacionesDbContext contexto = CrearContexto();

        var servicio = new ServicioProveedores(
            new RepositorioProveedores(contexto),
            new UnidadDeTrabajo(contexto),
            Reloj);

        return await servicio.ActualizarAsync(id, new GuardarProveedorDto { Nombre = nombre, Version = version });
    }
}
