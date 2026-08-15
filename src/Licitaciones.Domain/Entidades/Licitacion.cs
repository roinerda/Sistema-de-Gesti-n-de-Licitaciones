using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Enumeraciones;
using Licitaciones.Domain.Excepciones;
using Licitaciones.Domain.Normalizacion;
using Licitaciones.Domain.Servicios;

namespace Licitaciones.Domain.Entidades;

/// <summary>
/// Proceso de compra al que los proveedores presentan ofertas económicas.
/// </summary>
/// <remarks>
/// Concentra las reglas de estado, vencimiento y presupuesto descritas en las secciones 8.1, 8.2 y 8.5
/// del enunciado. Todos los montos se guardan en colones (CRC), que es la moneda oficial del sistema.
/// </remarks>
public sealed class Licitacion : EntidadBase, IBorradoLogico
{
    /// <summary>Longitud máxima admitida para el código.</summary>
    public const int LongitudMaximaCodigo = 40;

    /// <summary>Longitud máxima admitida para el título.</summary>
    public const int LongitudMaximaTitulo = 200;

    /// <summary>Constructor sin parámetros requerido por Entity Framework Core.</summary>
    private Licitacion()
    {
    }

    /// <summary>Código visible de la licitación, con espacios laterales ya recortados.</summary>
    public string Codigo { get; private set; } = string.Empty;

    /// <summary>Código normalizado que respalda el índice único.</summary>
    public string CodigoNormalizado { get; private set; } = string.Empty;

    /// <summary>Título descriptivo de la licitación.</summary>
    public string Titulo { get; private set; } = string.Empty;

    /// <summary>Estado actual dentro del ciclo de vida.</summary>
    public EstadoLicitacion Estado { get; private set; }

    /// <summary>Fecha y hora de cierre. Se almacena en UTC y se presenta en America/Costa_Rica.</summary>
    public DateTimeOffset FechaCierre { get; private set; }

    /// <summary>Presupuesto estimado en colones. Siempre mayor que cero.</summary>
    public decimal PresupuestoEstimadoCrc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>Indica si la licitación fue dada de baja lógicamente.</summary>
    public bool EstaEliminada => DeletedAt is not null;

    /// <summary>
    /// Crea una licitación en estado <see cref="EstadoLicitacion.Borrador"/>.
    /// </summary>
    /// <param name="codigo">Código único de la licitación.</param>
    /// <param name="titulo">Título descriptivo.</param>
    /// <param name="fechaCierre">Fecha y hora de cierre; debe ser futura.</param>
    /// <param name="presupuestoEstimadoCrc">Presupuesto en colones; debe ser mayor que cero.</param>
    /// <param name="ahora">Instante actual provisto por <see cref="IReloj"/>.</param>
    /// <returns>Licitación válida, aún no persistida.</returns>
    /// <exception cref="ReglaNegocioException">Si algún dato incumple las reglas de negocio.</exception>
    public static Licitacion Crear(
        string? codigo,
        string? titulo,
        DateTimeOffset fechaCierre,
        decimal presupuestoEstimadoCrc,
        DateTimeOffset ahora)
    {
        var licitacion = new Licitacion();
        licitacion.InicializarAuditoria(ahora);
        licitacion.Estado = EstadoLicitacion.Borrador;
        licitacion.AsignarCodigo(codigo);
        licitacion.AsignarTitulo(titulo);
        licitacion.AsignarFechaCierre(fechaCierre, ahora);
        licitacion.AsignarPresupuesto(presupuestoEstimadoCrc, montoOfertaMayorCrc: null);
        return licitacion;
    }

    /// <summary>
    /// Actualiza los datos editables de la licitación.
    /// </summary>
    /// <param name="codigo">Código único.</param>
    /// <param name="titulo">Título descriptivo.</param>
    /// <param name="fechaCierre">Nueva fecha y hora de cierre; debe ser futura.</param>
    /// <param name="presupuestoEstimadoCrc">Nuevo presupuesto en colones.</param>
    /// <param name="montoOfertaMayorCrc">
    /// Monto de la oferta más alta ya registrada, o <see langword="null"/> si no hay ofertas. El presupuesto
    /// no puede quedar por debajo de este valor (sección 8.5).
    /// </param>
    /// <param name="ahora">Instante actual provisto por <see cref="IReloj"/>.</param>
    /// <exception cref="ReglaNegocioException">Si la licitación ya está cerrada o algún dato es inválido.</exception>
    public void ActualizarDatos(
        string? codigo,
        string? titulo,
        DateTimeOffset fechaCierre,
        decimal presupuestoEstimadoCrc,
        decimal? montoOfertaMayorCrc,
        DateTimeOffset ahora)
    {
        GarantizarEditable(ahora);
        AsignarCodigo(codigo);
        AsignarTitulo(titulo);
        AsignarFechaCierre(fechaCierre, ahora);
        AsignarPresupuesto(presupuestoEstimadoCrc, montoOfertaMayorCrc);
        RegistrarActualizacion(ahora);
    }

    /// <summary>
    /// Aplica una transición de estado si la tabla de transiciones la permite.
    /// </summary>
    /// <param name="nuevoEstado">Estado destino.</param>
    /// <param name="ahora">Instante actual provisto por <see cref="IReloj"/>.</param>
    /// <exception cref="ReglaNegocioException">Si la transición no está permitida o faltan condiciones.</exception>
    public void CambiarEstado(EstadoLicitacion nuevoEstado, DateTimeOffset ahora)
    {
        if (EstaEliminada)
        {
            throw new ReglaNegocioException(
                CodigosError.LicitacionEliminada,
                "La licitación fue eliminada y no admite cambios de estado.");
        }

        if (!TransicionesLicitacion.EsPermitida(Estado, nuevoEstado))
        {
            throw new ReglaNegocioException(
                CodigosError.TransicionNoPermitida,
                $"No se permite pasar de {Estado} a {nuevoEstado}.");
        }

        if (nuevoEstado == EstadoLicitacion.Publicada)
        {
            GarantizarCondicionesDePublicacion(ahora);
        }

        Estado = nuevoEstado;
        RegistrarActualizacion(ahora);
    }

    /// <summary>
    /// Indica si la licitación está cerrada funcionalmente: por estado, por borrado lógico o porque ya se
    /// alcanzó la fecha de cierre aunque el campo de estado todavía diga <c>Publicada</c> (sección 8.1).
    /// </summary>
    /// <param name="ahora">Instante actual provisto por <see cref="IReloj"/>.</param>
    /// <returns><see langword="true"/> si la licitación ya no admite actividad.</returns>
    public bool EstaCerradaFuncionalmente(DateTimeOffset ahora) =>
        EstaEliminada || Estado == EstadoLicitacion.Cerrada || ahora >= FechaCierre;

    /// <summary>
    /// Indica si la licitación acepta ofertas nuevas o modificaciones de ofertas.
    /// </summary>
    /// <param name="ahora">Instante actual provisto por <see cref="IReloj"/>.</param>
    /// <returns><see langword="true"/> si está publicada, vigente y no eliminada.</returns>
    public bool AceptaOfertas(DateTimeOffset ahora) =>
        !EstaEliminada && Estado == EstadoLicitacion.Publicada && ahora < FechaCierre;

    /// <summary>
    /// Valida que la licitación admita registrar o modificar ofertas y explica el motivo del rechazo.
    /// </summary>
    /// <param name="ahora">Instante actual provisto por <see cref="IReloj"/>.</param>
    /// <exception cref="ReglaNegocioException">Si está eliminada, no publicada o vencida.</exception>
    public void GarantizarQueAceptaOfertas(DateTimeOffset ahora)
    {
        if (EstaEliminada)
        {
            throw new ReglaNegocioException(
                CodigosError.LicitacionEliminada,
                "La licitación fue eliminada y no admite ofertas.");
        }

        if (Estado != EstadoLicitacion.Publicada)
        {
            throw new ReglaNegocioException(
                CodigosError.OfertaLicitacionNoPublicada,
                "Solo se admiten ofertas en licitaciones publicadas.");
        }

        if (ahora >= FechaCierre)
        {
            throw new ReglaNegocioException(
                CodigosError.OfertaVencida,
                "La licitación alcanzó su fecha de cierre y no admite más movimientos de ofertas.");
        }
    }

    /// <summary>
    /// Aplica borrado lógico. Es idempotente.
    /// </summary>
    /// <param name="ahora">Instante actual provisto por <see cref="IReloj"/>.</param>
    public void Eliminar(DateTimeOffset ahora)
    {
        if (EstaEliminada)
        {
            return;
        }

        DeletedAt = ahora.ToUniversalTime();
        RegistrarActualizacion(ahora);
    }

    /// <summary>
    /// Revierte el borrado lógico.
    /// </summary>
    /// <param name="ahora">Instante actual provisto por <see cref="IReloj"/>.</param>
    public void Restaurar(DateTimeOffset ahora)
    {
        if (!EstaEliminada)
        {
            return;
        }

        DeletedAt = null;
        RegistrarActualizacion(ahora);
    }

    private void GarantizarEditable(DateTimeOffset ahora)
    {
        if (EstaEliminada)
        {
            throw new ReglaNegocioException(
                CodigosError.LicitacionEliminada,
                "La licitación fue eliminada y no admite modificaciones.");
        }

        if (Estado == EstadoLicitacion.Cerrada || ahora >= FechaCierre)
        {
            throw new ReglaNegocioException(
                CodigosError.LicitacionCerrada,
                "Una licitación cerrada no puede modificarse.");
        }
    }

    private void GarantizarCondicionesDePublicacion(DateTimeOffset ahora)
    {
        if (Titulo.Length == 0)
        {
            throw new ReglaNegocioException(
                CodigosError.TituloLicitacionRequerido,
                "No se puede publicar una licitación sin título.",
                nameof(Titulo));
        }

        if (PresupuestoEstimadoCrc <= 0m)
        {
            throw new ReglaNegocioException(
                CodigosError.PresupuestoInvalido,
                "No se puede publicar una licitación sin un presupuesto mayor que cero.",
                nameof(PresupuestoEstimadoCrc));
        }

        if (FechaCierre <= ahora)
        {
            throw new ReglaNegocioException(
                CodigosError.FechaCierreInvalida,
                "No se puede publicar una licitación cuya fecha de cierre ya pasó.",
                nameof(FechaCierre));
        }
    }

    private void AsignarCodigo(string? codigo)
    {
        string limpio = NormalizadorTexto.LimpiarEspacios(codigo);

        if (limpio.Length == 0)
        {
            throw new ReglaNegocioException(
                CodigosError.CodigoLicitacionRequerido,
                "El código de la licitación es obligatorio.",
                nameof(Codigo));
        }

        if (limpio.Length > LongitudMaximaCodigo)
        {
            throw new ReglaNegocioException(
                CodigosError.CodigoLicitacionLargo,
                $"El código no puede superar {LongitudMaximaCodigo} caracteres.",
                nameof(Codigo));
        }

        Codigo = limpio;
        CodigoNormalizado = NormalizadorTexto.NormalizarCodigo(limpio);
    }

    private void AsignarTitulo(string? titulo)
    {
        string limpio = NormalizadorTexto.LimpiarEspacios(titulo);

        if (limpio.Length == 0)
        {
            throw new ReglaNegocioException(
                CodigosError.TituloLicitacionRequerido,
                "El título de la licitación es obligatorio.",
                nameof(Titulo));
        }

        if (limpio.Length > LongitudMaximaTitulo)
        {
            throw new ReglaNegocioException(
                CodigosError.TituloLicitacionLargo,
                $"El título no puede superar {LongitudMaximaTitulo} caracteres.",
                nameof(Titulo));
        }

        Titulo = limpio;
    }

    private void AsignarFechaCierre(DateTimeOffset fechaCierre, DateTimeOffset ahora)
    {
        if (fechaCierre <= ahora)
        {
            throw new ReglaNegocioException(
                CodigosError.FechaCierreInvalida,
                "La fecha y hora de cierre debe ser posterior al momento actual.",
                nameof(FechaCierre));
        }

        FechaCierre = fechaCierre.ToUniversalTime();
    }

    private void AsignarPresupuesto(decimal presupuestoEstimadoCrc, decimal? montoOfertaMayorCrc)
    {
        if (presupuestoEstimadoCrc <= 0m)
        {
            throw new ReglaNegocioException(
                CodigosError.PresupuestoInvalido,
                "El presupuesto estimado debe ser mayor que cero.",
                nameof(PresupuestoEstimadoCrc));
        }

        if (montoOfertaMayorCrc is { } mayor && presupuestoEstimadoCrc < mayor)
        {
            throw new ReglaNegocioException(
                CodigosError.PresupuestoMenorAOferta,
                $"El presupuesto no puede quedar por debajo de la oferta ya registrada de {NormalizadorTexto.FormatearMonto(mayor)} CRC.",
                nameof(PresupuestoEstimadoCrc));
        }

        PresupuestoEstimadoCrc = decimal.Round(presupuestoEstimadoCrc, 2, MidpointRounding.AwayFromZero);
    }
}
