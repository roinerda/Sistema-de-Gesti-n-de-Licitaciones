using System.Diagnostics;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Licitaciones.Domain.Enumeraciones;
using Licitaciones.Web.Modelos;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controllers;

/// <summary>
/// Página inicial explicativa y páginas de servicio.
/// </summary>
public sealed class InicioController : ControladorWebBase
{
    private readonly IServicioLicitaciones _licitaciones;
    private readonly IServicioProveedores _proveedores;

    /// <summary>
    /// Crea el controlador.
    /// </summary>
    /// <param name="licitaciones">Casos de uso de licitaciones.</param>
    /// <param name="proveedores">Casos de uso de proveedores.</param>
    public InicioController(IServicioLicitaciones licitaciones, IServicioProveedores proveedores)
    {
        _licitaciones = licitaciones;
        _proveedores = proveedores;
    }

    /// <summary>
    /// Muestra la landing page con la explicación del flujo y un resumen del estado del sistema.
    /// </summary>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Vista de inicio.</returns>
    public async Task<IActionResult> Index(CancellationToken cancelacion)
    {
        PaginaResultado<LicitacionDto> publicadas = await _licitaciones.ListarAsync(
            new ParametrosConsultaLicitaciones { Estado = EstadoLicitacion.Publicada, TamanoPagina = 5 },
            cancelacion);

        PaginaResultado<LicitacionDto> todas = await _licitaciones.ListarAsync(
            new ParametrosConsultaLicitaciones { TamanoPagina = 1 },
            cancelacion);

        PaginaResultado<ProveedorDto> proveedores = await _proveedores.ListarAsync(
            new ParametrosConsultaProveedores { TamanoPagina = 1 },
            cancelacion);

        var modelo = new InicioVista
        {
            TotalLicitaciones = todas.TotalElementos,
            TotalProveedores = proveedores.TotalElementos,
            LicitacionesPublicadas = publicadas.Elementos,
        };

        return View(modelo);
    }

    /// <summary>
    /// Muestra una página de error controlada, sin detalles técnicos.
    /// </summary>
    /// <returns>Vista de error.</returns>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorVista
        {
            IdentificadorSolicitud = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
        });
}
