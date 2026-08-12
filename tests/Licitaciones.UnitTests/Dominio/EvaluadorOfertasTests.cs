using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Enumeraciones;
using Licitaciones.Domain.Servicios;
using Licitaciones.UnitTests.Comun;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>
/// Mejor oferta, ahorro y clasificación (historia H-11, sección 8.6 del enunciado).
/// </summary>
public sealed class EvaluadorOfertasTests
{
    private readonly RelojFalso _reloj = new();
    private readonly Licitacion _licitacion;

    public EvaluadorOfertasTests()
    {
        _licitacion = Licitacion.Crear("LIC-001", "Compra", _reloj.Ahora.AddDays(10), 1_000_000m, _reloj.Ahora);
        _licitacion.CambiarEstado(EstadoLicitacion.Publicada, _reloj.Ahora);
    }

    private Oferta Ofertar(string nombreProveedor, decimal monto)
    {
        var proveedor = Proveedor.Crear(nombreProveedor, _reloj.Ahora);
        return Oferta.Crear(_licitacion, proveedor, monto, _reloj.Ahora);
    }

    [Fact]
    public void ObtenerMejorOferta_SinOfertas_DevuelveNulo()
    {
        Assert.Null(EvaluadorOfertas.ObtenerMejorOferta([]));
    }

    [Fact]
    public void ObtenerMejorOferta_EligeElMontoMenor()
    {
        Oferta cara = Ofertar("Proveedor Uno", 900_000m);
        Oferta barata = Ofertar("Proveedor Dos", 700_000m);

        Oferta? mejor = EvaluadorOfertas.ObtenerMejorOferta([cara, barata]);

        Assert.Equal(barata.Id, mejor!.Id);
    }

    [Fact]
    public void ObtenerMejorOferta_EnEmpate_EligeLaRegistradaPrimero()
    {
        Oferta primera = Ofertar("Proveedor Uno", 800_000m);
        _reloj.Avanzar(TimeSpan.FromMinutes(5));
        Oferta segunda = Ofertar("Proveedor Dos", 800_000m);

        // Se pasan en orden inverso para comprobar que decide la fecha de registro, no el orden de entrada.
        Oferta? mejor = EvaluadorOfertas.ObtenerMejorOferta([segunda, primera]);

        Assert.Equal(primera.Id, mejor!.Id);
    }

    [Theory]
    [InlineData(900_000, 10.00, ClasificacionOferta.OfertaConveniente)]
    [InlineData(500_000, 50.00, ClasificacionOferta.OfertaConveniente)]
    [InlineData(899_999, 10.00, ClasificacionOferta.OfertaConveniente)]
    [InlineData(950_000, 5.00, ClasificacionOferta.OfertaAceptable)]
    [InlineData(999_000, 0.10, ClasificacionOferta.OfertaAceptable)]
    [InlineData(1_000_000, 0.00, ClasificacionOferta.OfertaValidaSinAhorro)]
    public void Evaluar_ClasificaSegunElAhorroObtenido(
        decimal mejorOferta,
        decimal ahorroEsperado,
        ClasificacionOferta clasificacionEsperada)
    {
        Oferta oferta = Ofertar("Proveedor Uno", mejorOferta);

        EvaluacionOfertas resultado = EvaluadorOfertas.Evaluar(1_000_000m, [oferta]);

        Assert.Equal(ahorroEsperado, resultado.PorcentajeAhorro);
        Assert.Equal(clasificacionEsperada, resultado.Clasificacion);
    }

    [Fact]
    public void Evaluar_ConAhorroMinusculo_SiguePresentandoloComoOfertaAceptable()
    {
        // ₡1 de ahorro sobre ₡1 000 000 equivale a 0,0001 %, que se redondea a 0,00 % al mostrarlo.
        // La clasificación debe seguir siendo «Oferta aceptable» porque el ahorro real es mayor que cero.
        Oferta oferta = Ofertar("Proveedor Uno", 999_999m);

        EvaluacionOfertas resultado = EvaluadorOfertas.Evaluar(1_000_000m, [oferta]);

        Assert.Equal(0.00m, resultado.PorcentajeAhorro);
        Assert.Equal(ClasificacionOferta.OfertaAceptable, resultado.Clasificacion);
    }

    [Fact]
    public void Evaluar_SinOfertas_DevuelveSinOfertasValidas()
    {
        EvaluacionOfertas resultado = EvaluadorOfertas.Evaluar(1_000_000m, []);

        Assert.Null(resultado.MejorOferta);
        Assert.Equal(0m, resultado.PorcentajeAhorro);
        Assert.Equal(ClasificacionOferta.SinOfertasValidas, resultado.Clasificacion);
        Assert.Equal("Sin ofertas válidas", resultado.Clasificacion.Descripcion());
    }

    [Fact]
    public void Evaluar_ConOfertaConveniente_DevuelveAhorroYDescripcion()
    {
        Oferta oferta = Ofertar("Proveedor Uno", 850_000m);

        EvaluacionOfertas resultado = EvaluadorOfertas.Evaluar(1_000_000m, [oferta]);

        Assert.Equal(oferta.Id, resultado.MejorOferta!.Id);
        Assert.Equal(15.00m, resultado.PorcentajeAhorro);
        Assert.Equal("Oferta conveniente", resultado.Clasificacion.Descripcion());
    }

    [Fact]
    public void CalcularPorcentajeAhorro_ConPresupuestoNoPositivo_Rechaza()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EvaluadorOfertas.CalcularPorcentajeAhorro(0m, 100m));
    }

    [Fact]
    public void CalcularPorcentajeAhorro_RedondeaADosDecimales()
    {
        decimal ahorro = EvaluadorOfertas.CalcularPorcentajeAhorro(3_000_000m, 2_000_000m);

        Assert.Equal(33.33m, ahorro);
    }

    [Theory]
    [InlineData(ClasificacionOferta.SinOfertasValidas, "Sin ofertas válidas")]
    [InlineData(ClasificacionOferta.OfertaConveniente, "Oferta conveniente")]
    [InlineData(ClasificacionOferta.OfertaAceptable, "Oferta aceptable")]
    [InlineData(ClasificacionOferta.OfertaValidaSinAhorro, "Oferta válida sin ahorro")]
    public void Descripcion_UsaElTextoExactoDelEnunciado(ClasificacionOferta clasificacion, string esperado)
    {
        Assert.Equal(esperado, clasificacion.Descripcion());
    }
}
