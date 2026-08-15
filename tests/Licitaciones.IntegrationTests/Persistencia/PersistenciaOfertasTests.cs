using Licitaciones.Application.Abstracciones;
using Licitaciones.Domain.Entidades;
using Licitaciones.Infrastructure.Persistencia;
using Licitaciones.IntegrationTests.Comun;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.IntegrationTests.Persistencia;

/// <summary>
/// Comprueba en el motor real las restricciones que protegen a las ofertas.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public sealed class PersistenciaOfertasTests : PruebaConBaseDatos
{
    public PersistenciaOfertasTests(ContenedorPostgres contenedor)
        : base(contenedor)
    {
    }

    [Fact]
    public async Task PostgreSQL_ImpideDosOfertasDelMismoProveedorEnLaMismaLicitacion()
    {
        Guid licitacionId = await SembrarLicitacionAsync("LIC-2026-001");
        Guid proveedorId = await SembrarProveedorAsync("Constructora Alfa");
        await SembrarOfertaAsync(licitacionId, proveedorId, 4_000_000m);

        ViolacionUnicidadException excepcion = await Assert.ThrowsAsync<ViolacionUnicidadException>(
            () => InsertarOfertaAsync(licitacionId, proveedorId, 3_500_000m));

        Assert.Equal("ux_ofertas_licitacion_proveedor", excepcion.NombreIndice);
    }

    [Fact]
    public async Task MismoProveedor_PuedeOfertarEnLicitacionesDistintas()
    {
        Guid primera = await SembrarLicitacionAsync("LIC-2026-001");
        Guid segunda = await SembrarLicitacionAsync("LIC-2026-002");
        Guid proveedorId = await SembrarProveedorAsync("Constructora Alfa");

        await SembrarOfertaAsync(primera, proveedorId, 4_000_000m);
        await SembrarOfertaAsync(segunda, proveedorId, 4_500_000m);

        await using LicitacionesDbContext contexto = CrearContexto();
        int total = await contexto.Ofertas.CountAsync(o => o.ProveedorId == proveedorId);

        Assert.Equal(2, total);
    }

    [Fact]
    public async Task MontoNoPositivo_EsRechazadoPorLaRestriccionCheck()
    {
        Guid licitacionId = await SembrarLicitacionAsync("LIC-2026-001");
        Guid proveedorId = await SembrarProveedorAsync("Constructora Alfa");
        Guid ofertaId = await SembrarOfertaAsync(licitacionId, proveedorId, 4_000_000m);

        Npgsql.PostgresException excepcion = await Assert.ThrowsAsync<Npgsql.PostgresException>(
            () => ConsultaSql.EjecutarAsync(
                CadenaConexion,
                "UPDATE ofertas SET monto_ofertado_crc = 0 WHERE id = $1",
                ofertaId));

        Assert.Equal("ck_ofertas_monto_positivo", excepcion.ConstraintName);
    }

    [Fact]
    public async Task EliminarUnProveedorConOfertas_SeTraduceAErrorDeIntegridadControlado()
    {
        Guid licitacionId = await SembrarLicitacionAsync("LIC-2026-001");
        Guid proveedorId = await SembrarProveedorAsync("Constructora Alfa");
        await SembrarOfertaAsync(licitacionId, proveedorId, 4_000_000m);

        await using LicitacionesDbContext contexto = CrearContexto();
        var unidadDeTrabajo = new UnidadDeTrabajo(contexto);

        Proveedor proveedor = await contexto.Proveedores.SingleAsync(p => p.Id == proveedorId);
        contexto.Proveedores.Remove(proveedor);

        ViolacionIntegridadException excepcion = await Assert.ThrowsAsync<ViolacionIntegridadException>(
            () => unidadDeTrabajo.GuardarCambiosAsync());

        Assert.Equal("fk_ofertas_proveedor", excepcion.NombreRestriccion);

        // El mensaje que llega a la aplicación no puede filtrar detalles técnicos del motor.
        Assert.DoesNotContain("23503", excepcion.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("SELECT", excepcion.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BorradoLogicoDelProveedor_ConservaSusOfertasComoEvidencia()
    {
        Guid licitacionId = await SembrarLicitacionAsync("LIC-2026-001");
        Guid proveedorId = await SembrarProveedorAsync("Constructora Alfa");
        Guid ofertaId = await SembrarOfertaAsync(licitacionId, proveedorId, 4_000_000m);

        await using (LicitacionesDbContext contexto = CrearContexto())
        {
            Proveedor proveedor = await contexto.Proveedores.SingleAsync(p => p.Id == proveedorId);
            proveedor.Eliminar(Reloj.Ahora);
            await contexto.SaveChangesAsync();
        }

        await using LicitacionesDbContext verificacion = CrearContexto();
        Oferta oferta = await verificacion.Ofertas
            .Include(o => o.Proveedor)
            .SingleAsync(o => o.Id == ofertaId);

        Assert.Equal(4_000_000m, oferta.MontoOfertadoCrc);
        Assert.True(oferta.Proveedor!.EstaEliminado);
    }

    [Fact]
    public async Task MontosCrc_SeGuardanConDosDecimalesExactosSinRedondeoBinario()
    {
        Guid licitacionId = await SembrarLicitacionAsync("LIC-2026-001", presupuestoCrc: 10_000_000m);
        Guid proveedorId = await SembrarProveedorAsync("Constructora Alfa");
        await SembrarOfertaAsync(licitacionId, proveedorId, 1_234_567.89m);

        await using LicitacionesDbContext contexto = CrearContexto();
        decimal monto = await contexto.Ofertas
            .Where(o => o.LicitacionId == licitacionId)
            .Select(o => o.MontoOfertadoCrc)
            .SingleAsync();

        Assert.Equal(1_234_567.89m, monto);
    }

    private async Task InsertarOfertaAsync(Guid licitacionId, Guid proveedorId, decimal montoCrc)
    {
        await using LicitacionesDbContext contexto = CrearContexto();
        var unidadDeTrabajo = new UnidadDeTrabajo(contexto);

        Licitacion licitacion = await contexto.Licitaciones.SingleAsync(l => l.Id == licitacionId);
        Proveedor proveedor = await contexto.Proveedores.SingleAsync(p => p.Id == proveedorId);

        contexto.Ofertas.Add(Oferta.Crear(licitacion, proveedor, montoCrc, Reloj.Ahora));
        await unidadDeTrabajo.GuardarCambiosAsync();
    }
}
