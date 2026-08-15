using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Licitaciones.IntegrationTests.Comun;
using Licitaciones.Infrastructure.Persistencia;
using Licitaciones.Infrastructure.Persistencia.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.IntegrationTests.Persistencia;

/// <summary>
/// Comprueba contra PostgreSQL que solo pueda existir un tipo de cambio activo.
/// </summary>
/// <remarks>
/// El índice único parcial <c>ux_tipos_cambio_activo</c> rechazaría dos filas activas, así que la
/// activación debe desactivar la anterior y confirmarla dentro de la misma transacción. Esta regla
/// no se puede verificar sin el motor real.
/// </remarks>
[Collection(ColeccionPostgres.Nombre)]
public sealed class PersistenciaTiposCambioTests : PruebaConBaseDatos
{
    public PersistenciaTiposCambioTests(ContenedorPostgres contenedor)
        : base(contenedor)
    {
    }

    [Fact]
    public async Task CrearOtroActivo_DesactivaElAnteriorSinViolarElIndiceUnico()
    {
        Resultado<TipoCambioDto> creado = await EnServicioAsync(servicio => servicio.CrearAsync(
            new GuardarTipoCambioDto
            {
                CrcPorUsd = 545.5000m,
                FechaVigencia = Reloj.Ahora,
                Activo = true,
            }));

        Assert.True(creado.EsExito);

        await using LicitacionesDbContext contexto = CrearContexto();
        var activos = await contexto.TiposCambio.Where(t => t.Activo).ToListAsync();

        Assert.Single(activos);
        Assert.Equal(545.5000m, activos[0].CrcPorUsd);
        Assert.Equal(2, await contexto.TiposCambio.CountAsync());
    }

    [Fact]
    public async Task Activar_UnRegistroInactivo_DejaExactamenteUnoActivo()
    {
        Resultado<TipoCambioDto> inactivo = await EnServicioAsync(servicio => servicio.CrearAsync(
            new GuardarTipoCambioDto
            {
                CrcPorUsd = 512.7500m,
                FechaVigencia = Reloj.Ahora.AddDays(-1),
                Activo = false,
            }));

        Assert.True(inactivo.EsExito);

        Guid idInactivo = inactivo.Valor!.Id;

        Resultado<TipoCambioDto> activado = await EnServicioAsync(
            servicio => servicio.ActivarAsync(idInactivo));

        Assert.True(activado.EsExito);

        await using LicitacionesDbContext contexto = CrearContexto();
        var activos = await contexto.TiposCambio.Where(t => t.Activo).ToListAsync();

        Assert.Single(activos);
        Assert.Equal(idInactivo, activos[0].Id);
    }

    [Fact]
    public async Task SegundaFilaActivaPorSqlDirecto_EsRechazadaPorElIndiceUnicoParcial()
    {
        Npgsql.PostgresException excepcion = await Assert.ThrowsAsync<Npgsql.PostgresException>(
            () => ConsultaSql.EjecutarAsync(
                CadenaConexion,
                """
                INSERT INTO tipos_cambio (id, crc_por_usd, fecha_vigencia, activo, version, created_at, updated_at)
                VALUES ($1, 600.0000, now(), true, 1, now(), now())
                """,
                Guid.NewGuid()));

        Assert.Equal("23505", excepcion.SqlState);
        Assert.Equal("ux_tipos_cambio_activo", excepcion.ConstraintName);
    }

    [Fact]
    public async Task ConversionAUsd_UsaElTipoDeCambioActivoEIndicaSuFecha()
    {
        await using LicitacionesDbContext contexto = CrearContexto();
        var servicio = new ServicioConversionMoneda(new RepositorioTiposCambio(contexto));

        Resultado<MontoConvertidoDto> resultado = await servicio.ConvertirAsync(1_040_000m);

        Assert.True(resultado.EsExito);
        Assert.Equal(520.0000m, resultado.Valor!.CrcPorUsd);
        Assert.Equal(2_000m, resultado.Valor.MontoUsd);
        Assert.NotEqual(default, resultado.Valor.FechaTipoCambio);
    }

    private async Task<T> EnServicioAsync<T>(Func<IServicioTiposCambio, Task<T>> operacion)
    {
        await using LicitacionesDbContext contexto = CrearContexto();

        var servicio = new ServicioTiposCambio(
            new RepositorioTiposCambio(contexto),
            new UnidadDeTrabajo(contexto),
            Reloj);

        return await operacion(servicio);
    }
}
