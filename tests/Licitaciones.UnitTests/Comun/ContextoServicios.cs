using Licitaciones.Application.Servicios;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Enumeraciones;

namespace Licitaciones.UnitTests.Comun;

/// <summary>
/// Arma los servicios de aplicación sobre repositorios en memoria y un reloj controlado.
/// </summary>
/// <remarks>
/// Cada prueba obtiene un contexto nuevo, por lo que no comparten estado y pueden ejecutarse en paralelo.
/// </remarks>
public sealed class ContextoServicios
{
    public ContextoServicios()
    {
        Almacen = new AlmacenEnMemoria();
        Reloj = new RelojFalso();

        var unidadDeTrabajo = new UnidadDeTrabajoEnMemoria(Almacen);
        var repositorioProveedores = new RepositorioProveedoresEnMemoria(Almacen);
        var repositorioLicitaciones = new RepositorioLicitacionesEnMemoria(Almacen);
        var repositorioOfertas = new RepositorioOfertasEnMemoria(Almacen);
        var repositorioNiveles = new RepositorioNivelesAprobacionEnMemoria(Almacen);
        var repositorioTiposCambio = new RepositorioTiposCambioEnMemoria(Almacen);

        Proveedores = new ServicioProveedores(repositorioProveedores, unidadDeTrabajo, Reloj);
        NivelesAprobacion = new ServicioNivelesAprobacion(repositorioNiveles, unidadDeTrabajo, Reloj);
        Licitaciones = new ServicioLicitaciones(
            repositorioLicitaciones,
            repositorioOfertas,
            NivelesAprobacion,
            unidadDeTrabajo,
            Reloj);
        Ofertas = new ServicioOfertas(
            repositorioOfertas,
            repositorioLicitaciones,
            repositorioProveedores,
            unidadDeTrabajo,
            Reloj);
        TiposCambio = new ServicioTiposCambio(repositorioTiposCambio, unidadDeTrabajo, Reloj);
        ConversionMoneda = new ServicioConversionMoneda(repositorioTiposCambio);
    }

    public AlmacenEnMemoria Almacen { get; }

    public RelojFalso Reloj { get; }

    public IServicioProveedores Proveedores { get; }

    public IServicioLicitaciones Licitaciones { get; }

    public IServicioOfertas Ofertas { get; }

    public IServicioNivelesAprobacion NivelesAprobacion { get; }

    public IServicioTiposCambio TiposCambio { get; }

    public IServicioConversionMoneda ConversionMoneda { get; }

    /// <summary>Inserta un proveedor directamente en el almacén, sin pasar por el caso de uso.</summary>
    public Proveedor SembrarProveedor(string nombre = "Empresa Central")
    {
        var proveedor = Proveedor.Crear(nombre, Reloj.Ahora);
        Almacen.Proveedores.Add(proveedor);
        return proveedor;
    }

    /// <summary>Inserta una licitación en el almacén, opcionalmente ya publicada.</summary>
    public Licitacion SembrarLicitacion(
        string codigo = "LIC-001",
        decimal presupuesto = 1_000_000m,
        int diasParaCierre = 10,
        bool publicada = true)
    {
        var licitacion = Licitacion.Crear(
            codigo,
            "Compra de equipo",
            Reloj.Ahora.AddDays(diasParaCierre),
            presupuesto,
            Reloj.Ahora);

        if (publicada)
        {
            licitacion.CambiarEstado(EstadoLicitacion.Publicada, Reloj.Ahora);
        }

        Almacen.Licitaciones.Add(licitacion);
        return licitacion;
    }

    /// <summary>Inserta una oferta en el almacén.</summary>
    public Oferta SembrarOferta(Licitacion licitacion, Proveedor proveedor, decimal monto)
    {
        var oferta = Oferta.Crear(licitacion, proveedor, monto, Reloj.Ahora);
        Almacen.Ofertas.Add(oferta);
        return oferta;
    }

    /// <summary>Inserta la tabla de niveles de aprobación del enunciado.</summary>
    public void SembrarNivelesDelEnunciado()
    {
        Almacen.NivelesAprobacion.AddRange(
        [
            NivelAprobacion.Crear(0.01m, 999_999.99m, "Encargado de área", Reloj.Ahora),
            NivelAprobacion.Crear(1_000_000m, 9_999_999.99m, "Gerencia", Reloj.Ahora),
            NivelAprobacion.Crear(10_000_000m, null, "Junta Directiva", Reloj.Ahora),
        ]);
    }

    /// <summary>Inserta un tipo de cambio activo.</summary>
    public TipoCambio SembrarTipoCambioActivo(decimal crcPorUsd = 520m)
    {
        var tipoCambio = TipoCambio.Crear(crcPorUsd, Reloj.Ahora, activo: true, Reloj.Ahora);
        Almacen.TiposCambio.Add(tipoCambio);
        return tipoCambio;
    }
}
