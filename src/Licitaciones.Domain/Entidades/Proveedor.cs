using Licitaciones.Domain.Comun;
using Licitaciones.Domain.Excepciones;
using Licitaciones.Domain.Normalizacion;

namespace Licitaciones.Domain.Entidades;

/// <summary>
/// Empresa o persona que puede presentar ofertas a una licitación.
/// </summary>
public sealed class Proveedor : EntidadBase, IBorradoLogico
{
    /// <summary>Longitud máxima admitida para el nombre.</summary>
    public const int LongitudMaximaNombre = 150;

    /// <summary>Constructor sin parámetros requerido por Entity Framework Core.</summary>
    private Proveedor()
    {
    }

    /// <summary>Nombre tal como se muestra, con espacios ya limpiados.</summary>
    public string Nombre { get; private set; } = string.Empty;

    /// <summary>Nombre normalizado que respalda el índice único de unicidad.</summary>
    public string NombreNormalizado { get; private set; } = string.Empty;

    /// <inheritdoc />
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>Indica si el proveedor fue dado de baja lógicamente.</summary>
    public bool EstaEliminado => DeletedAt is not null;

    /// <summary>
    /// Crea un proveedor validando el nombre.
    /// </summary>
    /// <param name="nombre">Nombre propuesto.</param>
    /// <param name="ahora">Instante actual provisto por <see cref="IReloj"/>.</param>
    /// <returns>Proveedor válido, aún no persistido.</returns>
    /// <exception cref="ReglaNegocioException">Si el nombre está vacío, excede la longitud o usa caracteres no permitidos.</exception>
    public static Proveedor Crear(string? nombre, DateTimeOffset ahora)
    {
        var proveedor = new Proveedor();
        proveedor.InicializarAuditoria(ahora);
        proveedor.AsignarNombre(nombre);
        return proveedor;
    }

    /// <summary>
    /// Cambia el nombre del proveedor.
    /// </summary>
    /// <param name="nombre">Nuevo nombre.</param>
    /// <param name="ahora">Instante actual provisto por <see cref="IReloj"/>.</param>
    /// <exception cref="ReglaNegocioException">Si el proveedor está eliminado o el nombre es inválido.</exception>
    public void Renombrar(string? nombre, DateTimeOffset ahora)
    {
        GarantizarVigente();
        AsignarNombre(nombre);
        RegistrarActualizacion(ahora);
    }

    /// <summary>
    /// Aplica borrado lógico. Es idempotente para no fallar ante reintentos.
    /// </summary>
    /// <param name="ahora">Instante actual provisto por <see cref="IReloj"/>.</param>
    public void Eliminar(DateTimeOffset ahora)
    {
        if (EstaEliminado)
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
        if (!EstaEliminado)
        {
            return;
        }

        DeletedAt = null;
        RegistrarActualizacion(ahora);
    }

    private void GarantizarVigente()
    {
        if (EstaEliminado)
        {
            throw new ReglaNegocioException(
                CodigosError.ProveedorEliminado,
                "El proveedor fue eliminado y no admite modificaciones.");
        }
    }

    private void AsignarNombre(string? nombre)
    {
        string limpio = NormalizadorTexto.LimpiarEspacios(nombre);

        if (limpio.Length == 0)
        {
            throw new ReglaNegocioException(
                CodigosError.NombreProveedorRequerido,
                "El nombre del proveedor es obligatorio.",
                nameof(Nombre));
        }

        if (limpio.Length > LongitudMaximaNombre)
        {
            throw new ReglaNegocioException(
                CodigosError.NombreProveedorLargo,
                $"El nombre del proveedor no puede superar {LongitudMaximaNombre} caracteres.",
                nameof(Nombre));
        }

        if (!PatronesTexto.NombreProveedor().IsMatch(limpio))
        {
            throw new ReglaNegocioException(
                CodigosError.NombreProveedorCaracteres,
                "El nombre del proveedor solo admite letras, números, espacios, punto, coma y paréntesis.",
                nameof(Nombre));
        }

        Nombre = limpio;
        NombreNormalizado = NormalizadorTexto.NormalizarNombre(limpio);
    }
}
