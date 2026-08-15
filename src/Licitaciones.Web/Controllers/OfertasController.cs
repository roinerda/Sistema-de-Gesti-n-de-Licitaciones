using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Licitaciones.Web.Modelos;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controllers;

/// <summary>
/// Interfaz web del módulo de ofertas.
/// </summary>
public sealed class OfertasController : ControladorWebBase
{
    private readonly IServicioOfertas _servicio;
    private readonly IServicioLicitaciones _licitaciones;
    private readonly IServicioProveedores _proveedores;

    /// <summary>
    /// Crea el controlador.
    /// </summary>
    /// <param name="servicio">Casos de uso de ofertas.</param>
    /// <param name="licitaciones">Casos de uso de licitaciones.</param>
    /// <param name="proveedores">Casos de uso de proveedores.</param>
    public OfertasController(
        IServicioOfertas servicio,
        IServicioLicitaciones licitaciones,
        IServicioProveedores proveedores)
    {
        _servicio = servicio;
        _licitaciones = licitaciones;
        _proveedores = proveedores;
    }

    /// <summary>Lista las ofertas con filtro por licitación y por proveedor.</summary>
    /// <param name="parametros">Parámetros de la consulta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Vista de listado.</returns>
    public async Task<IActionResult> Index(
        [FromQuery] ParametrosConsultaOfertas parametros,
        CancellationToken cancelacion)
    {
        ViewData["Parametros"] = parametros;
        ViewData["Licitaciones"] = (await _licitaciones.ListarAsync(
            new ParametrosConsultaLicitaciones { TamanoPagina = 100 },
            cancelacion)).Elementos;
        ViewData["Proveedores"] = (await _proveedores.ListarAsync(
            new ParametrosConsultaProveedores { TamanoPagina = 100 },
            cancelacion)).Elementos;

        return View(await _servicio.ListarAsync(parametros, cancelacion));
    }

    /// <summary>Muestra el formulario de registro de una oferta.</summary>
    /// <param name="licitacionId">Licitación preseleccionada, si viene desde su detalle.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Vista de creación.</returns>
    public async Task<IActionResult> Crear(Guid? licitacionId, CancellationToken cancelacion) =>
        View(await ConstruirFormularioAsync(
            new GuardarOfertaDto { LicitacionId = licitacionId ?? Guid.Empty },
            cancelacion));

    /// <summary>Registra una oferta.</summary>
    /// <param name="datos">Datos del formulario.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Redirección al detalle de la licitación o el formulario con los errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(GuardarOfertaDto datos, CancellationToken cancelacion)
    {
        if (!ModelState.IsValid)
        {
            return View(await ConstruirFormularioAsync(datos, cancelacion));
        }

        Resultado<OfertaDto> resultado = await _servicio.CrearAsync(datos, cancelacion);

        if (!resultado.EsExito)
        {
            AgregarErrorAlModelo(resultado.Error!);
            return View(await ConstruirFormularioAsync(datos, cancelacion));
        }

        AvisarExito("Oferta registrada correctamente.");
        return RedirectToAction("Detalle", "Licitaciones", new { id = datos.LicitacionId });
    }

    /// <summary>Muestra el formulario de edición del monto.</summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Vista de edición.</returns>
    public async Task<IActionResult> Editar(Guid id, CancellationToken cancelacion)
    {
        Resultado<OfertaDto> resultado = await _servicio.ObtenerAsync(id, cancelacion);

        if (!resultado.EsExito)
        {
            return ResponderNoEncontrado(resultado.Error!);
        }

        ViewData["Oferta"] = resultado.Valor;

        return View(new ActualizarOfertaDto
        {
            MontoOfertadoCrc = resultado.Valor!.MontoOfertadoCrc,
            Version = resultado.Valor.Version,
        });
    }

    /// <summary>Guarda el nuevo monto de una oferta.</summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="datos">Datos del formulario.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Redirección al detalle de la licitación o el formulario con los errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Guid id, ActualizarOfertaDto datos, CancellationToken cancelacion)
    {
        Resultado<OfertaDto> actual = await _servicio.ObtenerAsync(id, cancelacion);

        if (!actual.EsExito)
        {
            return ResponderNoEncontrado(actual.Error!);
        }

        ViewData["Oferta"] = actual.Valor;

        if (!ModelState.IsValid)
        {
            return View(datos);
        }

        Resultado<OfertaDto> resultado = await _servicio.ActualizarAsync(id, datos, cancelacion);

        if (!resultado.EsExito)
        {
            AgregarErrorAlModelo(resultado.Error!);
            return View(datos);
        }

        AvisarExito("Oferta actualizada correctamente.");
        return RedirectToAction("Detalle", "Licitaciones", new { id = resultado.Valor!.LicitacionId });
    }

    /// <summary>Muestra la confirmación de eliminación.</summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Vista de confirmación.</returns>
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        Resultado<OfertaDto> resultado = await _servicio.ObtenerAsync(id, cancelacion);

        return resultado.EsExito
            ? View(resultado.Valor)
            : ResponderNoEncontrado(resultado.Error!);
    }

    /// <summary>Elimina la oferta tras la confirmación.</summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Redirección al listado de ofertas.</returns>
    [HttpPost]
    [ActionName(nameof(Eliminar))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarEliminacion(Guid id, CancellationToken cancelacion)
    {
        Resultado resultado = await _servicio.EliminarAsync(id, cancelacion);

        if (resultado.EsExito)
        {
            AvisarExito("Oferta eliminada correctamente.");
        }
        else
        {
            AvisarError(resultado.Error!.Mensaje);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<OfertaFormularioVista> ConstruirFormularioAsync(
        GuardarOfertaDto datos,
        CancellationToken cancelacion)
    {
        PaginaResultado<LicitacionDto> licitaciones = await _licitaciones.ListarAsync(
            new ParametrosConsultaLicitaciones { TamanoPagina = 100 },
            cancelacion);

        PaginaResultado<ProveedorDto> proveedores = await _proveedores.ListarAsync(
            new ParametrosConsultaProveedores { TamanoPagina = 100 },
            cancelacion);

        return new OfertaFormularioVista
        {
            Datos = datos,
            // Solo se ofrecen licitaciones que realmente admiten ofertas: publicadas y no vencidas.
            Licitaciones = licitaciones.Elementos
                .Where(l => l.Estado == Domain.Enumeraciones.EstadoLicitacion.Publicada && !l.CerradaFuncionalmente)
                .ToList(),
            Proveedores = proveedores.Elementos,
        };
    }
}
