using Licitaciones.Application.Comun;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controllers;

/// <summary>
/// Base de los controladores MVC: traduce errores de aplicación a mensajes de la interfaz.
/// </summary>
/// <remarks>
/// Mantiene los controladores delgados y garantiza que un error de negocio se muestre junto al campo que
/// lo provocó cuando el error identifica un campo, y como aviso general cuando no.
/// </remarks>
public abstract class ControladorWebBase : Controller
{
    /// <summary>Clave de <c>TempData</c> con el mensaje de éxito de la operación anterior.</summary>
    public const string ClaveMensajeExito = "MensajeExito";

    /// <summary>Clave de <c>TempData</c> con el mensaje de error de la operación anterior.</summary>
    public const string ClaveMensajeError = "MensajeError";

    /// <summary>
    /// Registra un mensaje de éxito que se mostrará después de redirigir.
    /// </summary>
    /// <param name="mensaje">Texto que verá la persona usuaria.</param>
    protected void AvisarExito(string mensaje) => TempData[ClaveMensajeExito] = mensaje;

    /// <summary>
    /// Registra un mensaje de error que se mostrará después de redirigir.
    /// </summary>
    /// <param name="mensaje">Texto que verá la persona usuaria.</param>
    protected void AvisarError(string mensaje) => TempData[ClaveMensajeError] = mensaje;

    /// <summary>
    /// Traslada un error de aplicación al estado del modelo.
    /// </summary>
    /// <param name="error">Error devuelto por el caso de uso.</param>
    protected void AgregarErrorAlModelo(ErrorApp error)
    {
        ArgumentNullException.ThrowIfNull(error);

        // El nombre del campo lo decide el dominio y no siempre coincide con el del formulario:
        // la licitación valida «FechaCierre» mientras el formulario enlaza «FechaCierreLocal»,
        // porque el navegador envía la fecha sin desplazamiento horario. Si la clave no está
        // enlazada, el mensaje va al resumen de validación en lugar de a un campo inexistente,
        // donde no lo mostraría ninguna vista: un error invisible es peor que uno mal colocado.
        string clave = error.Campo is { Length: > 0 } campo && ModelState.ContainsKey(campo)
            ? campo
            : string.Empty;

        ModelState.AddModelError(clave, error.Mensaje);
    }

    /// <summary>
    /// Devuelve una vista de error controlada cuando no se encuentra un recurso.
    /// </summary>
    /// <param name="error">Error devuelto por el caso de uso.</param>
    /// <returns>Resultado de acción con el código HTTP adecuado.</returns>
    protected IActionResult ResponderNoEncontrado(ErrorApp error)
    {
        ArgumentNullException.ThrowIfNull(error);
        AvisarError(error.Mensaje);
        return RedirectToAction("Index");
    }
}
