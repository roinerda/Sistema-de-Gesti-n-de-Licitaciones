using Licitaciones.Domain.Enumeraciones;
using Licitaciones.Domain.Servicios;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>
/// Matriz completa de transiciones de estado (historia H-07, sección 8.1 del enunciado).
/// </summary>
public sealed class TransicionesLicitacionTests
{
    [Theory]
    [InlineData(EstadoLicitacion.Borrador, EstadoLicitacion.Publicada, true)]
    [InlineData(EstadoLicitacion.Borrador, EstadoLicitacion.Cerrada, true)]
    [InlineData(EstadoLicitacion.Publicada, EstadoLicitacion.Cerrada, true)]
    [InlineData(EstadoLicitacion.Borrador, EstadoLicitacion.Borrador, false)]
    [InlineData(EstadoLicitacion.Publicada, EstadoLicitacion.Borrador, false)]
    [InlineData(EstadoLicitacion.Publicada, EstadoLicitacion.Publicada, false)]
    [InlineData(EstadoLicitacion.Cerrada, EstadoLicitacion.Borrador, false)]
    [InlineData(EstadoLicitacion.Cerrada, EstadoLicitacion.Publicada, false)]
    [InlineData(EstadoLicitacion.Cerrada, EstadoLicitacion.Cerrada, false)]
    public void EsPermitida_CubreLaMatrizCompletaDeEstados(
        EstadoLicitacion origen,
        EstadoLicitacion destino,
        bool esperado)
    {
        Assert.Equal(esperado, TransicionesLicitacion.EsPermitida(origen, destino));
    }

    [Fact]
    public void DestinosDesde_Borrador_PermitePublicarYCerrar()
    {
        IReadOnlyCollection<EstadoLicitacion> destinos = TransicionesLicitacion.DestinosDesde(EstadoLicitacion.Borrador);

        Assert.Equal(2, destinos.Count);
        Assert.Contains(EstadoLicitacion.Publicada, destinos);
        Assert.Contains(EstadoLicitacion.Cerrada, destinos);
    }

    [Fact]
    public void DestinosDesde_Publicada_SoloPermiteCerrar()
    {
        IReadOnlyCollection<EstadoLicitacion> destinos = TransicionesLicitacion.DestinosDesde(EstadoLicitacion.Publicada);

        Assert.Equal([EstadoLicitacion.Cerrada], destinos);
    }

    [Fact]
    public void DestinosDesde_Cerrada_EsUnEstadoTerminal()
    {
        Assert.Empty(TransicionesLicitacion.DestinosDesde(EstadoLicitacion.Cerrada));
    }
}
