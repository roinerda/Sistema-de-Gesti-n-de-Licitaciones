using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Excepciones;
using Licitaciones.UnitTests.Comun;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>
/// Reglas de la entidad <see cref="Proveedor"/> (historias H-01 y H-04).
/// </summary>
public sealed class ProveedorTests
{
    private readonly RelojFalso _reloj = new();

    [Fact]
    public void Crear_ConNombreValido_LimpiaEspaciosYGeneraIdentificador()
    {
        var proveedor = Proveedor.Crear("  Constructora   del   Valle  ", _reloj.Ahora);

        Assert.NotEqual(Guid.Empty, proveedor.Id);
        Assert.Equal("Constructora del Valle", proveedor.Nombre);
        Assert.Equal("CONSTRUCTORA DEL VALLE", proveedor.NombreNormalizado);
        Assert.Equal(_reloj.Ahora, proveedor.CreatedAt);
        Assert.Equal(_reloj.Ahora, proveedor.UpdatedAt);
        Assert.Equal(1, proveedor.Version);
        Assert.False(proveedor.EstaEliminado);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinNombre_RechazaConCodigoRequerido(string? nombre)
    {
        var excepcion = Assert.Throws<ReglaNegocioException>(() => Proveedor.Crear(nombre, _reloj.Ahora));

        Assert.Equal(CodigosError.NombreProveedorRequerido, excepcion.Codigo);
    }

    [Fact]
    public void Crear_ConCaracteresNoPermitidos_RechazaConCodigoDeCaracteres()
    {
        var excepcion = Assert.Throws<ReglaNegocioException>(() => Proveedor.Crear("Empresa #1", _reloj.Ahora));

        Assert.Equal(CodigosError.NombreProveedorCaracteres, excepcion.Codigo);
        Assert.Equal(nameof(Proveedor.Nombre), excepcion.Campo);
    }

    [Fact]
    public void Crear_ConNombreDemasiadoLargo_Rechaza()
    {
        string nombre = new('A', Proveedor.LongitudMaximaNombre + 1);

        var excepcion = Assert.Throws<ReglaNegocioException>(() => Proveedor.Crear(nombre, _reloj.Ahora));

        Assert.Equal(CodigosError.NombreProveedorLargo, excepcion.Codigo);
    }

    [Fact]
    public void Renombrar_ActualizaNombreMarcaDeTiempoYVersion()
    {
        var proveedor = Proveedor.Crear("Empresa Central", _reloj.Ahora);
        _reloj.Avanzar(TimeSpan.FromHours(2));

        proveedor.Renombrar("Empresa Central del Sur", _reloj.Ahora);

        Assert.Equal("Empresa Central del Sur", proveedor.Nombre);
        Assert.Equal("EMPRESA CENTRAL DEL SUR", proveedor.NombreNormalizado);
        Assert.Equal(_reloj.Ahora, proveedor.UpdatedAt);
        Assert.Equal(2, proveedor.Version);
    }

    [Fact]
    public void Eliminar_AplicaBorradoLogicoYEsIdempotente()
    {
        var proveedor = Proveedor.Crear("Empresa Central", _reloj.Ahora);
        _reloj.Avanzar(TimeSpan.FromMinutes(30));

        proveedor.Eliminar(_reloj.Ahora);
        DateTimeOffset? primeraBaja = proveedor.DeletedAt;

        _reloj.Avanzar(TimeSpan.FromMinutes(30));
        proveedor.Eliminar(_reloj.Ahora);

        Assert.True(proveedor.EstaEliminado);
        Assert.Equal(primeraBaja, proveedor.DeletedAt);
    }

    [Fact]
    public void Renombrar_ProveedorEliminado_Rechaza()
    {
        var proveedor = Proveedor.Crear("Empresa Central", _reloj.Ahora);
        proveedor.Eliminar(_reloj.Ahora);

        var excepcion = Assert.Throws<ReglaNegocioException>(() =>
            proveedor.Renombrar("Otro Nombre", _reloj.Ahora));

        Assert.Equal(CodigosError.ProveedorEliminado, excepcion.Codigo);
    }

    [Fact]
    public void Restaurar_DevuelveElProveedorAlEstadoVigente()
    {
        var proveedor = Proveedor.Crear("Empresa Central", _reloj.Ahora);
        proveedor.Eliminar(_reloj.Ahora);

        proveedor.Restaurar(_reloj.Ahora);

        Assert.False(proveedor.EstaEliminado);
        Assert.Null(proveedor.DeletedAt);
    }
}
