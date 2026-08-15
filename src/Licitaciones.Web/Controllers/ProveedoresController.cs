using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controllers;

/// <summary>
/// Interfaz web del módulo de proveedores.
/// </summary>
public sealed class ProveedoresController : ControladorWebBase
{
    private readonly IServicioProveedores _servicio;
    private readonly IServicioOfertas _ofertas;

    /// <summary>
    /// Crea el controlador.
    /// </summary>
    /// <param name="servicio">Casos de uso de proveedores.</param>
    /// <param name="ofertas">Casos de uso de ofertas.</param>
    public ProveedoresController(IServicioProveedores servicio, IServicioOfertas ofertas)
    {
        _servicio = servicio;
        _ofertas = ofertas;
    }

    /// <summary>Lista los proveedores con paginación, búsqueda y ordenamiento.</summary>
    /// <param name="parametros">Parámetros de la consulta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Vista de listado.</returns>
    public async Task<IActionResult> Index(
        [FromQuery] ParametrosConsultaProveedores parametros,
        CancellationToken cancelacion)
    {
        ViewData["Parametros"] = parametros;
        return View(await _servicio.ListarAsync(parametros, cancelacion));
    }

    /// <summary>Muestra el detalle de un proveedor junto con sus ofertas.</summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Vista de detalle.</returns>
    public async Task<IActionResult> Detalle(Guid id, CancellationToken cancelacion)
    {
        Resultado<ProveedorDto> resultado = await _servicio.ObtenerAsync(id, cancelacion);

        if (!resultado.EsExito)
        {
            return ResponderNoEncontrado(resultado.Error!);
        }

        ViewData["Ofertas"] = await _ofertas.ListarAsync(
            new ParametrosConsultaOfertas { ProveedorId = id, TamanoPagina = 50 },
            cancelacion);

        return View(resultado.Valor);
    }

    /// <summary>Muestra el formulario de alta.</summary>
    /// <returns>Vista de creación.</returns>
    public IActionResult Crear() => View(new GuardarProveedorDto());

    /// <summary>Registra un proveedor.</summary>
    /// <param name="datos">Datos del formulario.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Redirección al listado o el formulario con los errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(GuardarProveedorDto datos, CancellationToken cancelacion)
    {
        if (!ModelState.IsValid)
        {
            return View(datos);
        }

        Resultado<ProveedorDto> resultado = await _servicio.CrearAsync(datos, cancelacion);

        if (!resultado.EsExito)
        {
            AgregarErrorAlModelo(resultado.Error!);
            return View(datos);
        }

        AvisarExito($"Proveedor «{resultado.Valor!.Nombre}» registrado correctamente.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Muestra el formulario de edición.</summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Vista de edición.</returns>
    public async Task<IActionResult> Editar(Guid id, CancellationToken cancelacion)
    {
        Resultado<ProveedorDto> resultado = await _servicio.ObtenerAsync(id, cancelacion);

        if (!resultado.EsExito)
        {
            return ResponderNoEncontrado(resultado.Error!);
        }

        ViewData["Id"] = id;

        return View(new GuardarProveedorDto
        {
            Nombre = resultado.Valor!.Nombre,
            Version = resultado.Valor.Version,
        });
    }

    /// <summary>Guarda los cambios de un proveedor.</summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="datos">Datos del formulario.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Redirección al listado o el formulario con los errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Guid id, GuardarProveedorDto datos, CancellationToken cancelacion)
    {
        ViewData["Id"] = id;

        if (!ModelState.IsValid)
        {
            return View(datos);
        }

        Resultado<ProveedorDto> resultado = await _servicio.ActualizarAsync(id, datos, cancelacion);

        if (!resultado.EsExito)
        {
            AgregarErrorAlModelo(resultado.Error!);
            return View(datos);
        }

        AvisarExito($"Proveedor «{resultado.Valor!.Nombre}» actualizado correctamente.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Muestra la confirmación de eliminación.</summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Vista de confirmación.</returns>
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        Resultado<ProveedorDto> resultado = await _servicio.ObtenerAsync(id, cancelacion);

        return resultado.EsExito
            ? View(resultado.Valor)
            : ResponderNoEncontrado(resultado.Error!);
    }

    /// <summary>Aplica el borrado lógico tras la confirmación.</summary>
    /// <param name="id">Identificador del proveedor.</param>
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
            AvisarExito("Proveedor eliminado. Sus ofertas se conservan como evidencia.");
        }
        else
        {
            AvisarError(resultado.Error!.Mensaje);
        }

        return RedirectToAction(nameof(Index));
    }
}
