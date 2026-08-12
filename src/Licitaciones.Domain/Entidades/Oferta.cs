using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Excepciones;
using Licitaciones.Domain.Normalizacion;

namespace Licitaciones.Domain.Entidades;

/// <summary>
/// Propuesta económica que un proveedor presenta a una licitación.
/// </summary>
/// <remarks>
/// Un proveedor solo puede tener una oferta por licitación; la unicidad se garantiza además con un índice
/// único compuesto en PostgreSQL (sección 8.3).
/// </remarks>
public sealed class Oferta : EntidadBase
{
    /// <summary>Constructor sin parámetros requerido por Entity Framework Core.</summary>
    private Oferta()
    {
    }

    /// <summary>Licitación a la que pertenece la oferta.</summary>
    public Guid LicitacionId { get; private set; }

    /// <summary>Proveedor que presenta la oferta.</summary>
    public Guid ProveedorId { get; private set; }

    /// <summary>Monto ofertado en colones. Siempre mayor que cero y menor o igual al presupuesto.</summary>
    public decimal MontoOfertadoCrc { get; private set; }

    /// <summary>Instante en que la oferta fue registrada; se usa para desempatar la mejor oferta.</summary>
    public DateTimeOffset FechaRegistro { get; private set; }

    /// <summary>Referencia de navegación a la licitación.</summary>
    public Licitacion? Licitacion { get; private set; }

    /// <summary>Referencia de navegación al proveedor.</summary>
    public Proveedor? Proveedor { get; private set; }

    /// <summary>
    /// Registra una oferta para una licitación publicada y vigente.
    /// </summary>
    /// <param name="licitacion">Licitación destino, ya cargada.</param>
    /// <param name="proveedor">Proveedor que oferta, ya cargado.</param>
    /// <param name="montoOfertadoCrc">Monto en colones.</param>
    /// <param name="ahora">Instante actual provisto por <see cref="IReloj"/>.</param>
    /// <returns>Oferta válida, aún no persistida.</returns>
    /// <exception cref="ReglaNegocioException">
    /// Si la licitación no acepta ofertas, el proveedor está eliminado, el monto no es positivo o supera
    /// el presupuesto estimado.
    /// </exception>
    public static Oferta Crear(Licitacion licitacion, Proveedor proveedor, decimal montoOfertadoCrc, DateTimeOffset ahora)
    {
        ArgumentNullException.ThrowIfNull(licitacion);
        ArgumentNullException.ThrowIfNull(proveedor);

        licitacion.GarantizarQueAceptaOfertas(ahora);
        GarantizarProveedorVigente(proveedor);

        var oferta = new Oferta();
        oferta.InicializarAuditoria(ahora);
        oferta.LicitacionId = licitacion.Id;
        oferta.ProveedorId = proveedor.Id;
        oferta.FechaRegistro = ahora.ToUniversalTime();
        oferta.AsignarMonto(montoOfertadoCrc, licitacion.PresupuestoEstimadoCrc);
        return oferta;
    }

    /// <summary>
    /// Modifica el monto de una oferta existente.
    /// </summary>
    /// <param name="licitacion">Licitación asociada, ya cargada.</param>
    /// <param name="montoOfertadoCrc">Nuevo monto en colones.</param>
    /// <param name="ahora">Instante actual provisto por <see cref="IReloj"/>.</param>
    /// <exception cref="ReglaNegocioException">Si la licitación ya no admite movimientos o el monto es inválido.</exception>
    public void ActualizarMonto(Licitacion licitacion, decimal montoOfertadoCrc, DateTimeOffset ahora)
    {
        ArgumentNullException.ThrowIfNull(licitacion);

        licitacion.GarantizarQueAceptaOfertas(ahora);
        AsignarMonto(montoOfertadoCrc, licitacion.PresupuestoEstimadoCrc);
        RegistrarActualizacion(ahora);
    }

    private static void GarantizarProveedorVigente(Proveedor proveedor)
    {
        if (proveedor.EstaEliminado)
        {
            throw new ReglaNegocioException(
                CodigosError.ProveedorEliminado,
                "El proveedor fue eliminado y no puede presentar ofertas.");
        }
    }

    private void AsignarMonto(decimal montoOfertadoCrc, decimal presupuestoEstimadoCrc)
    {
        if (montoOfertadoCrc <= 0m)
        {
            throw new ReglaNegocioException(
                CodigosError.MontoOfertaInvalido,
                "El monto ofertado debe ser mayor que cero.",
                nameof(MontoOfertadoCrc));
        }

        decimal redondeado = decimal.Round(montoOfertadoCrc, 2, MidpointRounding.AwayFromZero);

        // Una oferta igual al presupuesto es válida; solo se rechaza cuando lo supera (sección 8.5).
        if (redondeado > presupuestoEstimadoCrc)
        {
            throw new ReglaNegocioException(
                CodigosError.OfertaSuperaPresupuesto,
                $"La oferta no puede superar el presupuesto estimado de {NormalizadorTexto.FormatearMonto(presupuestoEstimadoCrc)} CRC.",
                nameof(MontoOfertadoCrc));
        }

        MontoOfertadoCrc = redondeado;
    }
}
