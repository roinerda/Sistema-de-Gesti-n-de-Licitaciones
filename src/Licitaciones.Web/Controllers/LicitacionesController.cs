using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Licitaciones.Domain.Enumeraciones;
using Licitaciones.Web.Modelos;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controllers;

/// <summary>
/// Interfaz web del módulo de licitaciones.
/// </summary>
public sealed class LicitacionesController : ControladorWebBase
{
    private readonly IServicioLicitaciones _servicio;
    private readonly IServicioOfertas _ofertas;

    /// <summary>
    /// Crea el controlador.
    /// </summary>
    /// <param name="servicio">Casos de uso de licitaciones.</param>
    /// <param name="ofertas">Casos de uso de ofertas.</param>
    public LicitacionesController(IServicioLicitaciones servicio, IServicioOfertas ofertas)
    {
        _servicio = servicio;
        _ofertas = ofertas;
    }

    /// <summary>Lista las licitaciones con paginación, filtro por estado y ordenamiento.</summary>
    /// <param name="parametros">Parámetros de la consulta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Vista de listado.</returns>
    public async Task<IActionResult> Index(
        [FromQuery] ParametrosConsultaLicitaciones parametros,
        CancellationToken cancelacion)
    {
        ViewData["Parametros"] = parametros;
        return View(await _servicio.ListarAsync(parametros, cancelacion));
    }

    /// <summary>Muestra el detalle con ofertas, mejor oferta, clasificación y aprobador.</summary>
    /// <param name="id">Identificador de la licitación.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Vista de detalle.</returns>
    public async Task<IActionResult> Detalle(Guid id, CancellationToken cancelacion)
    {
        Resultado<LicitacionDetalleDto> resultado = await _servicio.ObtenerAsync(id, cancelacion);

        if (!resultado.EsExito)
        {
            return ResponderNoEncontrado(resultado.Error!);
        }

        ViewData["Ofertas"] = await _ofertas.ListarAsync(
            new ParametrosConsultaOfertas { LicitacionId = id, TamanoPagina = 50 },
            cancelacion);

        return View(resultado.Valor);
    }

    /// <summary>Muestra el formulario de alta con la fecha de cierre por omisión.</summary>
    /// <returns>Vista de creación.</returns>
    public IActionResult Crear() =>
        View(new LicitacionFormularioVista
        {
            FechaCierreLocal = ZonaHorariaCostaRica.AHoraLocal(DateTimeOffset.UtcNow).DateTime.AddDays(15),
        });

    /// <summary>Crea una licitación en estado borrador.</summary>
    /// <param name="datos">Datos del formulario.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Redirección al detalle o el formulario con los errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(LicitacionFormularioVista datos, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(datos);

        if (!ModelState.IsValid)
        {
            return View(datos);
        }

        Resultado<LicitacionDto> resultado = await _servicio.CrearAsync(datos.AGuardar(), cancelacion);

        if (!resultado.EsExito)
        {
            AgregarErrorAlModelo(resultado.Error!);
            return View(datos);
        }

        AvisarExito($"Licitación «{resultado.Valor!.Codigo}» creada en estado Borrador.");
        return RedirectToAction(nameof(Detalle), new { id = resultado.Valor.Id });
    }

    /// <summary>Muestra el formulario de edición.</summary>
    /// <param name="id">Identificador de la licitación.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Vista de edición.</returns>
    public async Task<IActionResult> Editar(Guid id, CancellationToken cancelacion)
    {
        Resultado<LicitacionDetalleDto> resultado = await _servicio.ObtenerAsync(id, cancelacion);

        if (!resultado.EsExito)
        {
            return ResponderNoEncontrado(resultado.Error!);
        }

        ViewData["Id"] = id;
        return View(LicitacionFormularioVista.Desde(resultado.Valor!.Licitacion));
    }

    /// <summary>Guarda los cambios de una licitación.</summary>
    /// <param name="id">Identificador de la licitación.</param>
    /// <param name="datos">Datos del formulario.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Redirección al detalle o el formulario con los errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Guid id, LicitacionFormularioVista datos, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(datos);
        ViewData["Id"] = id;

        if (!ModelState.IsValid)
        {
            return View(datos);
        }

        Resultado<LicitacionDto> resultado = await _servicio.ActualizarAsync(id, datos.AGuardar(), cancelacion);

        if (!resultado.EsExito)
        {
            AgregarErrorAlModelo(resultado.Error!);
            return View(datos);
        }

        AvisarExito($"Licitación «{resultado.Valor!.Codigo}» actualizada correctamente.");
        return RedirectToAction(nameof(Detalle), new { id });
    }

    /// <summary>Aplica una transición de estado.</summary>
    /// <param name="id">Identificador de la licitación.</param>
    /// <param name="nuevoEstado">Estado destino.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Redirección al detalle.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(
        Guid id,
        EstadoLicitacion nuevoEstado,
        CancellationToken cancelacion)
    {
        Resultado<LicitacionDto> resultado = await _servicio.CambiarEstadoAsync(
            id,
            new CambiarEstadoLicitacionDto { NuevoEstado = nuevoEstado },
            cancelacion);

        if (resultado.EsExito)
        {
            AvisarExito($"La licitación pasó al estado {resultado.Valor!.EstadoDescripcion}.");
        }
        else
        {
            AvisarError(resultado.Error!.Mensaje);
        }

        return RedirectToAction(nameof(Detalle), new { id });
    }

    /// <summary>Muestra la confirmación de eliminación.</summary>
    /// <param name="id">Identificador de la licitación.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Vista de confirmación.</returns>
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        Resultado<LicitacionDetalleDto> resultado = await _servicio.ObtenerAsync(id, cancelacion);

        return resultado.EsExito
            ? View(resultado.Valor!.Licitacion)
            : ResponderNoEncontrado(resultado.Error!);
    }

    /// <summary>Aplica el borrado lógico tras la confirmación.</summary>
    /// <param name="id">Identificador de la licitación.</param>
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
            AvisarExito("Licitación eliminada. Sus ofertas se conservan como evidencia.");
        }
        else
        {
            AvisarError(resultado.Error!.Mensaje);
        }

        return RedirectToAction(nameof(Index));
    }
}
