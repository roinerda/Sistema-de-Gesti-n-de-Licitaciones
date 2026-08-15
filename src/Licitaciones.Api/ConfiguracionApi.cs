using System.Reflection;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Licitaciones.Api.Comun;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Licitaciones.Api;

/// <summary>
/// Registro y publicación de la capa REST dentro del host web.
/// </summary>
public static class ConfiguracionApi
{
    /// <summary>Versión actual de la API.</summary>
    public const string VersionActual = "1.0";

    /// <summary>
    /// Registra controladores REST, versionado, documentación OpenAPI y manejo global de excepciones.
    /// </summary>
    /// <param name="servicios">Colección de servicios de la aplicación.</param>
    /// <returns>La misma colección, para encadenar llamadas.</returns>
    public static IServiceCollection AgregarApiRest(this IServiceCollection servicios)
    {
        ArgumentNullException.ThrowIfNull(servicios);

        servicios
            .AddControllers()
            .AddApplicationPart(typeof(ConfiguracionApi).Assembly)
            .AddJsonOptions(opciones =>
            {
                // Las enumeraciones viajan como texto ("Publicada") en lugar de como número: el contrato
                // queda legible y no se rompe si cambian los valores numéricos internos.
                opciones.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        // Los errores de validación del modelo se devuelven como ProblemDetails con el mismo formato
        // que los errores de negocio, incluyendo el identificador de correlación.
        servicios.Configure<ApiBehaviorOptions>(opciones =>
        {
            opciones.InvalidModelStateResponseFactory = contexto =>
            {
                var problema = new ValidationProblemDetails(contexto.ModelState)
                {
                    Title = "Datos de entrada inválidos",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "Revise los campos indicados y vuelva a enviar la solicitud.",
                    Instance = contexto.HttpContext.Request.Path,
                };

                problema.Extensions["codigoError"] = Domain.Comun.CodigosError.ValidacionEntrada;
                problema.Extensions["identificadorCorrelacion"] = contexto.HttpContext.TraceIdentifier;

                return new BadRequestObjectResult(problema)
                {
                    ContentTypes = { "application/problem+json" },
                };
            };
        });

        servicios.AddProblemDetails();
        servicios.AddExceptionHandler<ManejadorExcepcionesGlobal>();

        servicios
            .AddApiVersioning(opciones =>
            {
                opciones.DefaultApiVersion = new ApiVersion(1, 0);
                opciones.AssumeDefaultVersionWhenUnspecified = true;
                opciones.ReportApiVersions = true;
                opciones.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(opciones =>
            {
                opciones.GroupNameFormat = "'v'VVV";
                opciones.SubstituteApiVersionInUrl = true;
            });

        servicios.AddEndpointsApiExplorer();
        servicios.AddSwaggerGen(opciones =>
        {
            opciones.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "API del Sistema de Gestión de Licitaciones",
                Version = "v1",
                Description =
                    "Operaciones sobre licitaciones, proveedores, ofertas, niveles de aprobación y tipos de cambio. " +
                    "Todos los montos se expresan en colones costarricenses (CRC); la conversión a dólares es " +
                    "referencial y no modifica los valores almacenados.",
            });

            IncluirDocumentacionXml(opciones);
        });

        return servicios;
    }

    /// <summary>
    /// Publica los endpoints REST y la documentación interactiva.
    /// </summary>
    /// <param name="aplicacion">Aplicación web en construcción.</param>
    /// <returns>La misma aplicación, para encadenar llamadas.</returns>
    public static WebApplication UsarApiRest(this WebApplication aplicacion)
    {
        ArgumentNullException.ThrowIfNull(aplicacion);

        aplicacion.UseExceptionHandler();
        aplicacion.UseSwagger();
        aplicacion.UseSwaggerUI(opciones =>
        {
            IApiVersionDescriptionProvider proveedor =
                aplicacion.Services.GetRequiredService<IApiVersionDescriptionProvider>();

            foreach (ApiVersionDescription descripcion in proveedor.ApiVersionDescriptions)
            {
                opciones.SwaggerEndpoint(
                    $"/swagger/{descripcion.GroupName}/swagger.json",
                    $"Licitaciones {descripcion.GroupName.ToUpperInvariant()}");
            }

            opciones.DocumentTitle = "API · Sistema de Gestión de Licitaciones";
        });

        aplicacion.MapControllers();
        return aplicacion;
    }

    private static void IncluirDocumentacionXml(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions opciones)
    {
        // La documentación XML de los controladores y de los DTO alimenta las descripciones de Swagger,
        // de modo que el contrato publicado y los comentarios del código nunca se contradigan.
        foreach (Assembly ensamblado in new[] { typeof(ConfiguracionApi).Assembly, typeof(Application.ConfiguracionAplicacion).Assembly })
        {
            string ruta = Path.Combine(AppContext.BaseDirectory, $"{ensamblado.GetName().Name}.xml");

            if (File.Exists(ruta))
            {
                opciones.IncludeXmlComments(ruta, includeControllerXmlComments: true);
            }
        }
    }
}
