namespace Licitaciones.Domain.Comun;

/// <summary>
/// Códigos estables de error de negocio.
/// </summary>
/// <remarks>
/// Se exponen en las respuestas <c>ProblemDetails</c> de la API y permiten que la interfaz web y las
/// pruebas verifiquen la causa exacta de un rechazo sin depender del texto del mensaje.
/// </remarks>
public static class CodigosError
{
    // Generales
    public const string NoEncontrado = "RECURSO_NO_ENCONTRADO";
    public const string Concurrencia = "CONFLICTO_CONCURRENCIA";
    public const string ValidacionEntrada = "VALIDACION_ENTRADA";

    // Proveedor
    public const string NombreProveedorRequerido = "PROVEEDOR_NOMBRE_REQUERIDO";
    public const string NombreProveedorLargo = "PROVEEDOR_NOMBRE_DEMASIADO_LARGO";
    public const string NombreProveedorCaracteres = "PROVEEDOR_NOMBRE_CARACTERES_NO_PERMITIDOS";
    public const string NombreProveedorDuplicado = "PROVEEDOR_NOMBRE_DUPLICADO";
    public const string ProveedorConOfertas = "PROVEEDOR_CON_OFERTAS";
    public const string ProveedorEliminado = "PROVEEDOR_ELIMINADO";

    // Licitación
    public const string CodigoLicitacionRequerido = "LICITACION_CODIGO_REQUERIDO";
    public const string CodigoLicitacionLargo = "LICITACION_CODIGO_DEMASIADO_LARGO";
    public const string CodigoLicitacionDuplicado = "LICITACION_CODIGO_DUPLICADO";
    public const string TituloLicitacionRequerido = "LICITACION_TITULO_REQUERIDO";
    public const string TituloLicitacionLargo = "LICITACION_TITULO_DEMASIADO_LARGO";
    public const string PresupuestoInvalido = "LICITACION_PRESUPUESTO_INVALIDO";
    public const string PresupuestoMenorAOferta = "LICITACION_PRESUPUESTO_MENOR_A_OFERTA_EXISTENTE";
    public const string FechaCierreInvalida = "LICITACION_FECHA_CIERRE_INVALIDA";
    public const string TransicionNoPermitida = "LICITACION_TRANSICION_NO_PERMITIDA";
    public const string LicitacionCerrada = "LICITACION_CERRADA";
    public const string LicitacionEliminada = "LICITACION_ELIMINADA";
    public const string LicitacionConOfertas = "LICITACION_CON_OFERTAS";

    // Oferta
    public const string MontoOfertaInvalido = "OFERTA_MONTO_INVALIDO";
    public const string OfertaSuperaPresupuesto = "OFERTA_SUPERA_PRESUPUESTO";
    public const string OfertaDuplicada = "OFERTA_DUPLICADA";
    public const string OfertaLicitacionNoPublicada = "OFERTA_LICITACION_NO_PUBLICADA";
    public const string OfertaVencida = "OFERTA_VENCIDA";

    // Nivel de aprobación
    public const string RangoAprobacionInvalido = "NIVEL_APROBACION_RANGO_INVALIDO";
    public const string RangoAprobacionTraslapado = "NIVEL_APROBACION_RANGO_TRASLAPADO";
    public const string RangoAbiertoDuplicado = "NIVEL_APROBACION_RANGO_ABIERTO_DUPLICADO";
    public const string AprobadorRequerido = "NIVEL_APROBACION_APROBADOR_REQUERIDO";

    // Tipo de cambio
    public const string TipoCambioInvalido = "TIPO_CAMBIO_INVALIDO";
    public const string TipoCambioActivoRequerido = "TIPO_CAMBIO_ACTIVO_REQUERIDO";
    public const string TipoCambioActivoNoEliminable = "TIPO_CAMBIO_ACTIVO_NO_ELIMINABLE";
}
