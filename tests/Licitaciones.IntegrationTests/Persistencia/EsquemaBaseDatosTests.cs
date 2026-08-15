using Licitaciones.Domain.Enumeraciones;
using Licitaciones.IntegrationTests.Comun;
using Licitaciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.IntegrationTests.Persistencia;

/// <summary>
/// Verifica que las migraciones produzcan en PostgreSQL el esquema que exige el enunciado.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public sealed class EsquemaBaseDatosTests : PruebaConBaseDatos
{
    public EsquemaBaseDatosTests(ContenedorPostgres contenedor)
        : base(contenedor)
    {
    }

    [Fact]
    public async Task Migraciones_SeAplicanYNoDejanPendientes()
    {
        await using LicitacionesDbContext contexto = CrearContexto();

        IEnumerable<string> aplicadas = await contexto.Database.GetAppliedMigrationsAsync();
        IEnumerable<string> pendientes = await contexto.Database.GetPendingMigrationsAsync();

        Assert.NotEmpty(aplicadas);
        Assert.Empty(pendientes);
    }

    [Theory]
    [InlineData("licitaciones", "presupuesto_estimado_crc")]
    [InlineData("ofertas", "monto_ofertado_crc")]
    [InlineData("niveles_aprobacion", "monto_minimo_crc")]
    [InlineData("niveles_aprobacion", "monto_maximo_crc")]
    public async Task ColumnasMonetarias_UsanNumericConDosDecimales(string tabla, string columna)
    {
        string? tipo = await ConsultaSql.TextoAsync(
            CadenaConexion,
            """
            SELECT data_type || '(' || numeric_precision || ',' || numeric_scale || ')'
            FROM information_schema.columns
            WHERE table_name = $1 AND column_name = $2
            """,
            tabla,
            columna);

        // El enunciado prohíbe float y double para dinero: el redondeo binario alteraría los montos.
        Assert.Equal("numeric(18,2)", tipo);
    }

    [Fact]
    public async Task TipoCambio_UsaCuatroDecimalesPorSerUnFactorDeConversion()
    {
        string? tipo = await ConsultaSql.TextoAsync(
            CadenaConexion,
            """
            SELECT data_type || '(' || numeric_precision || ',' || numeric_scale || ')'
            FROM information_schema.columns
            WHERE table_name = 'tipos_cambio' AND column_name = 'crc_por_usd'
            """);

        Assert.Equal("numeric(18,4)", tipo);
    }

    [Theory]
    [InlineData("ux_proveedores_nombre_normalizado", "deleted_at IS NULL")]
    [InlineData("ux_licitaciones_codigo_normalizado", "deleted_at IS NULL")]
    [InlineData("ux_tipos_cambio_activo", "activo")]
    public async Task IndicesUnicosParciales_ExistenConSuFiltro(string indice, string fragmentoFiltro)
    {
        string? definicion = await ConsultaSql.TextoAsync(
            CadenaConexion,
            "SELECT indexdef FROM pg_indexes WHERE indexname = $1",
            indice);

        Assert.NotNull(definicion);
        Assert.Contains("UNIQUE", definicion, StringComparison.Ordinal);
        Assert.Contains(fragmentoFiltro, definicion, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IndiceUnicoDeOfertas_ImpideDosOfertasDelMismoProveedor()
    {
        string? definicion = await ConsultaSql.TextoAsync(
            CadenaConexion,
            "SELECT indexdef FROM pg_indexes WHERE indexname = 'ux_ofertas_licitacion_proveedor'");

        Assert.NotNull(definicion);
        Assert.Contains("UNIQUE", definicion, StringComparison.Ordinal);
        Assert.Contains("licitacion_id", definicion, StringComparison.Ordinal);
        Assert.Contains("proveedor_id", definicion, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ck_licitaciones_presupuesto_positivo")]
    [InlineData("ck_licitaciones_codigo_no_vacio")]
    [InlineData("ck_ofertas_monto_positivo")]
    [InlineData("ck_proveedores_nombre_no_vacio")]
    [InlineData("ck_tipos_cambio_valor_positivo")]
    [InlineData("ck_niveles_aprobacion_minimo_positivo")]
    [InlineData("ck_niveles_aprobacion_rango_coherente")]
    public async Task RestriccionesCheck_EstanDeclaradasEnElMotor(string restriccion)
    {
        bool existe = await ConsultaSql.ExisteAsync(
            CadenaConexion,
            "SELECT 1 FROM pg_constraint WHERE conname = $1 AND contype = 'c'",
            restriccion);

        Assert.True(existe, $"Falta la restricción CHECK {restriccion}.");
    }

    [Fact]
    public async Task ClavesForaneas_DeOfertasApuntanALicitacionYProveedor()
    {
        bool licitacion = await ConsultaSql.ExisteAsync(
            CadenaConexion,
            "SELECT 1 FROM pg_constraint WHERE conname = 'fk_ofertas_licitacion' AND contype = 'f'");

        bool proveedor = await ConsultaSql.ExisteAsync(
            CadenaConexion,
            "SELECT 1 FROM pg_constraint WHERE conname = 'fk_ofertas_proveedor' AND contype = 'f'");

        Assert.True(licitacion);
        Assert.True(proveedor);
    }

    [Fact]
    public async Task Semillas_CarganLosTresEstadosDelCicloDeVida()
    {
        await using LicitacionesDbContext contexto = CrearContexto();

        List<EstadoLicitacion> estados = await contexto.EstadosLicitacion
            .OrderBy(e => e.Id)
            .Select(e => e.Id)
            .ToListAsync();

        Assert.Equal(
            [EstadoLicitacion.Borrador, EstadoLicitacion.Publicada, EstadoLicitacion.Cerrada],
            estados);
    }

    [Fact]
    public async Task Semillas_CarganLaTablaDeNivelesDeAprobacionDelEnunciado()
    {
        await using LicitacionesDbContext contexto = CrearContexto();

        var niveles = await contexto.NivelesAprobacion
            .OrderBy(n => n.MontoMinimoCrc)
            .Select(n => new { n.MontoMinimoCrc, n.MontoMaximoCrc, n.Aprobador })
            .ToListAsync();

        Assert.Collection(
            niveles,
            primero =>
            {
                Assert.Equal(0.01m, primero.MontoMinimoCrc);
                Assert.Equal(999_999.99m, primero.MontoMaximoCrc);
                Assert.Equal("Encargado de área", primero.Aprobador);
            },
            segundo =>
            {
                Assert.Equal(1_000_000m, segundo.MontoMinimoCrc);
                Assert.Equal(9_999_999.99m, segundo.MontoMaximoCrc);
                Assert.Equal("Gerencia", segundo.Aprobador);
            },
            tercero =>
            {
                Assert.Equal(10_000_000m, tercero.MontoMinimoCrc);
                Assert.Null(tercero.MontoMaximoCrc);
                Assert.Equal("Junta Directiva", tercero.Aprobador);
            });
    }

    [Fact]
    public async Task Semillas_DejanUnTipoDeCambioActivoParaOperarSinInternet()
    {
        await using LicitacionesDbContext contexto = CrearContexto();

        var activos = await contexto.TiposCambio.Where(t => t.Activo).ToListAsync();

        Assert.Single(activos);
        Assert.Equal(520.0000m, activos[0].CrcPorUsd);
    }

    [Fact]
    public async Task MarcasDeTiempo_SeGuardanEnUtc()
    {
        Guid id = await SembrarProveedorAsync("Servicios Integrados");

        await using LicitacionesDbContext contexto = CrearContexto();
        var proveedor = await contexto.Proveedores.SingleAsync(p => p.Id == id);

        Assert.Equal(TimeSpan.Zero, proveedor.CreatedAt.Offset);
        Assert.Equal(TimeSpan.Zero, proveedor.UpdatedAt.Offset);
    }
}
