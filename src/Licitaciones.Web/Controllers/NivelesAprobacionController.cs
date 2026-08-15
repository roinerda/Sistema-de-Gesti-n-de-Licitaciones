using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controllers;

/// <summary>
/// Interfaz web del módulo de niveles de aprobación.
/// </summary>
public sealed class NivelesAprobacionController : ControladorWebBase
{
    private readonly IServicioNivelesAprobacion _servicio;

    /// <summary>
    /// Crea el controlador.
    /// </summary>
    /// <param name="servicio">Casos de uso de niveles de aprobación.</param>
    public NivelesAprobacionController(IServicioNivelesAprobacion servicio) => _servicio = servicio;

    /// <summary>Lista los niveles configurados.</summary>
    /// <param name="parametros">Parámetros de la consulta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Vista de listado.</returns>
    public async Task<IActionResult> Index([FromQuery] ParametrosConsulta parametros, CancellationToken cancelacion)
    {
        ViewData["Parametros"] = parametros;
        return View(await _servicio.ListarAsync(parametros, cancelacion));
    }

    /// <summary>Muestra el formulario de alta.</summary>
    /// <returns>Vista de creación.</returns>
    public IActionResult Crear() => View(new GuardarNivelAprobacionDto());

    /// <summary>Crea un nivel de aprobación.</summary>
    /// <param name="datos">Datos del formulario.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Redirección al listado o el formulario con los errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(GuardarNivelAprobacionDto datos, CancellationToken cancelacion)
    {
        if (!ModelState.IsValid)
        {
            return View(datos);
        }

        Resultado<NivelAprobacionDto> resultado = await _servicio.CrearAsync(datos, cancelacion);

        if (!resultado.EsExito)
        {
            AgregarErrorAlModelo(resultado.Error!);
            return View(datos);
        }

        AvisarExito($"Nivel de aprobación «{resultado.Valor!.Aprobador}» registrado.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Muestra el formulario de edición.</summary>
    /// <param name="id">Identificador del nivel.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Vista de edición.</returns>
    public async Task<IActionResult> Editar(Guid id, CancellationToken cancelacion)
    {
        Resultado<NivelAprobacionDto> resultado = await _servicio.ObtenerAsync(id, cancelacion);

        if (!resultado.EsExito)
        {
            return ResponderNoEncontrado(resultado.Error!);
        }

        ViewData["Id"] = id;

        return View(new GuardarNivelAprobacionDto
        {
            MontoMinimoCrc = resultado.Valor!.MontoMinimoCrc,
            MontoMaximoCrc = resultado.Valor.MontoMaximoCrc,
            Aprobador = resultado.Valor.Aprobador,
            Version = resultado.Valor.Version,
        });
    }

    /// <summary>Guarda los cambios de un nivel de aprobación.</summary>
    /// <param name="id">Identificador del nivel.</param>
    /// <param name="datos">Datos del formulario.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Redirección al listado o el formulario con los errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Guid id, GuardarNivelAprobacionDto datos, CancellationToken cancelacion)
    {
        ViewData["Id"] = id;

        if (!ModelState.IsValid)
        {
            return View(datos);
        }

        Resultado<NivelAprobacionDto> resultado = await _servicio.ActualizarAsync(id, datos, cancelacion);

        if (!resultado.EsExito)
        {
            AgregarErrorAlModelo(resultado.Error!);
            return View(datos);
        }

        AvisarExito("Nivel de aprobación actualizado.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Muestra la confirmación de eliminación.</summary>
    /// <param name="id">Identificador del nivel.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Vista de confirmación.</returns>
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        Resultado<NivelAprobacionDto> resultado = await _servicio.ObtenerAsync(id, cancelacion);

        return resultado.EsExito
            ? View(resultado.Valor)
            : ResponderNoEncontrado(resultado.Error!);
    }

    /// <summary>Elimina el nivel tras la confirmación.</summary>
    /// <param name="id">Identificador del nivel.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Redirección al listado.</returns>
    [HttpPost]
    [ActionName(nameof(Eliminar))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarEliminacion(Guid id, CancellationToken cancelacion)
    {
        Resultado resultado = await _servicio.EliminarAsync(id, cancelacion);

        if (resultado.EsExito)
        {
            AvisarExito("Nivel de aprobación eliminado.");
        }
        else
        {
            AvisarError(resultado.Error!.Mensaje);
        }

        return RedirectToAction(nameof(Index));
    }
}
