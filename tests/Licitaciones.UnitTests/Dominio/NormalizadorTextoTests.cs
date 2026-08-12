using Licitaciones.Domain.Normalizacion;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>
/// Verifica la normalización que respalda las reglas de unicidad (sección 8.3 del enunciado).
/// </summary>
public sealed class NormalizadorTextoTests
{
    [Theory]
    [InlineData("Empresa Central")]
    [InlineData("empresa central")]
    [InlineData("EMPRESA   CENTRAL")]
    [InlineData("  Empresa   Central  ")]
    public void NormalizarNombre_ConVariantesEquivalentes_ProduceElMismoValor(string entrada)
    {
        string resultado = NormalizadorTexto.NormalizarNombre(entrada);

        Assert.Equal("EMPRESA CENTRAL", resultado);
    }

    [Theory]
    [InlineData("lic-001")]
    [InlineData("LIC-001")]
    [InlineData("  lic-001  ")]
    public void NormalizarCodigo_IgnoraEspaciosLateralesYMayusculas(string entrada)
    {
        string resultado = NormalizadorTexto.NormalizarCodigo(entrada);

        Assert.Equal("LIC-001", resultado);
    }

    [Fact]
    public void NormalizarNombre_ConEntradaVaciaONula_DevuelveCadenaVacia()
    {
        Assert.Equal(string.Empty, NormalizadorTexto.NormalizarNombre(null));
        Assert.Equal(string.Empty, NormalizadorTexto.NormalizarNombre("   "));
    }

    [Fact]
    public void LimpiarEspacios_ConservaMayusculasYColapsaEspacios()
    {
        string resultado = NormalizadorTexto.LimpiarEspacios("  Constructora   del   Valle  ");

        Assert.Equal("Constructora del Valle", resultado);
    }

    [Theory]
    [InlineData("Empresa Central", true)]
    [InlineData("Constructora del Valle S.A.", true)]
    [InlineData("Servicios (Costa Rica), 2026", true)]
    [InlineData("Empresa #1", false)]
    [InlineData("Ñandú & Asociados", false)]
    [InlineData("Datos <script>", false)]
    public void NombreProveedor_ValidaSoloLosCaracteresPermitidos(string nombre, bool esValido)
    {
        Assert.Equal(esValido, PatronesTexto.NombreProveedor().IsMatch(nombre));
    }
}
