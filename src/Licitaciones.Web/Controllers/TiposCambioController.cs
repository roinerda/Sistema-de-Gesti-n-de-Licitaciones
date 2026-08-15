using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Licitaciones.Web.Modelos;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controllers;

/// <summary>
/// Interfaz web del módulo de tipos de cambio.
/// </summary>
public sealed class TiposCambioController : ControladorWebBase
{
    private readonly IServicioTiposCambio _servicio;

    /// <summary>
    /// Crea el controlador.
    /// </summary>
    /// <param name="servicio">Casos de uso de tipos de cambio.</param>
    public TiposCambioController(IServicioTiposCambio servicio) => _servicio = servicio;

    /// <summary>Lista los tipos de cambio registrados.</summary>
    /// <param name="parametros">Parámetros de la consulta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Vista de listado.</returns>
    public async Task<IActionResult> Index([FromQuery] ParametrosConsulta parametros, CancellationToken cancelacion)
    {
        ViewData["Parametros"] = parametros;
        return View(await _servicio.ListarAsync(parametros, cancelacion));
    }

    /// <summary>Muestra el formulario de alta con la fecha de vigencia de hoy.</summary>
    /// <returns>Vista de creación.</returns>
    public IActionResult Crear() =>
        View(new TipoCambioFormularioVista
        {
            FechaVigenciaLocal = ZonaHorariaCostaRica.AHoraLocal(DateTimeOffset.UtcNow).Date,
            Activo = true,
        });

    /// <summary>Registra un tipo de cambio.</summary>
    /// <param name="datos">Datos del formulario.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Redirección al listado o el formulario con los errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(TipoCambioFormularioVista datos, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(datos);

        if (!ModelState.IsValid)
        {
            return View(datos);
        }

        Resultado<TipoCambioDto> resultado = await _servicio.CrearAsync(datos.AGuardar(), cancelacion);

        if (!resultado.EsExito)
        {
            AgregarErrorAlModelo(resultado.Error!);
            return View(datos);
        }

        AvisarExito("Tipo de cambio registrado.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Muestra el formulario de edición.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Vista de edición.</returns>
    public async Task<IActionResult> Editar(Guid id, CancellationToken cancelacion)
    {
        Resultado<TipoCambioDto> resultado = await _servicio.ObtenerAsync(id, cancelacion);

        if (!resultado.EsExito)
        {
            return ResponderNoEncontrado(resultado.Error!);
        }

        ViewData["Id"] = id;
        return View(TipoCambioFormularioVista.Desde(resultado.Valor!));
    }

    /// <summary>Guarda los cambios de un tipo de cambio.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="datos">Datos del formulario.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Redirección al listado o el formulario con los errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Guid id, TipoCambioFormularioVista datos, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(datos);
        ViewData["Id"] = id;

        if (!ModelState.IsValid)
        {
            return View(datos);
        }

        Resultado<TipoCambioDto> resultado = await _servicio.ActualizarAsync(id, datos.AGuardar(), cancelacion);

        if (!resultado.EsExito)
        {
            AgregarErrorAlModelo(resultado.Error!);
            return View(datos);
        }

        AvisarExito("Tipo de cambio actualizado.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Marca un tipo de cambio como activo.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Redirección al listado.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activar(Guid id, CancellationToken cancelacion)
    {
        Resultado<TipoCambioDto> resultado = await _servicio.ActivarAsync(id, cancelacion);

        if (resultado.EsExito)
        {
            AvisarExito("Tipo de cambio activado. Los montos en dólares ya usan este valor.");
        }
        else
        {
            AvisarError(resultado.Error!.Mensaje);
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Muestra la confirmación de eliminación.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Vista de confirmación.</returns>
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        Resultado<TipoCambioDto> resultado = await _servicio.ObtenerAsync(id, cancelacion);

        return resultado.EsExito
            ? View(resultado.Valor)
            : ResponderNoEncontrado(resultado.Error!);
    }

    /// <summary>Elimina el tipo de cambio tras la confirmación.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
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
            AvisarExito("Tipo de cambio eliminado.");
        }
        else
        {
            AvisarError(resultado.Error!.Mensaje);
        }

        return RedirectToAction(nameof(Index));
    }
}
