# Módulo: API REST

Historia cubierta: H-18. Código principal: `src/Licitaciones.Api/`.

El catálogo completo de endpoints, con ejemplos y la colección reproducible, está en
[../api.md](../api.md). Este documento explica **cómo está construida**.

## 1. Una biblioteca, no un ejecutable

`Licitaciones.Api` se compila como biblioteca de clases con `FrameworkReference` a
`Microsoft.AspNetCore.App`, y el host web la monta con `AddApplicationPart`:

```csharp
servicios.AddControllers()
         .AddApplicationPart(typeof(ConfiguracionApi).Assembly);
```

El resultado es un solo proceso, una sola imagen y un solo conjunto de manifiestos de Kubernetes, con
la interfaz en `/` y la API en `/api/v1`. Dos ejecutables habrían duplicado configuración, sondas de
salud, migraciones y despliegue sin ganar nada: ambos consumen exactamente los mismos casos de uso.

## 2. Controladores delgados

Un controlador hace tres cosas: recibir el DTO, llamar al caso de uso y traducir el resultado a HTTP.
Ninguna regla de negocio vive aquí.

```csharp
[HttpPost]
public async Task<ActionResult<ProveedorDto>> Crear(
    GuardarProveedorDto datos,
    CancellationToken cancelacion) =>
    ResponderCreado(await _servicio.CrearAsync(datos, cancelacion), RutaObtener, new { id = ... });
```

La traducción está centralizada en `ControladorApiBase`, de modo que todos los endpoints devuelven
los mismos códigos y el mismo formato de error. Si estuviera repetida en cada controlador, la
diferencia entre `409` y `422` acabaría dependiendo de quién escribió cada método.

| `TipoError` | HTTP |
| ----- | ----- |
| `NoEncontrado` | 404 |
| `Conflicto` | 409 |
| `Concurrencia` | 409 |
| `Validacion` | 422 |
| `ReglaNegocio` | 422 |

## 3. Nunca se exponen entidades de EF Core

Todo entra y sale como DTO. Dos razones:

- Cambiar el modelo persistente no rompe a los clientes.
- Una entidad con navegaciones podría arrastrar medio grafo en la serialización, o entrar en un ciclo.

Los DTO de lectura son `record` posicionales con proyección explícita desde la entidad; los de
escritura son clases con anotaciones de validación.

## 4. Versionado

`Asp.Versioning` con `UrlSegmentApiVersionReader`: la versión viaja en la ruta
(`/api/v{version:apiVersion}/...`). La respuesta incluye `api-supported-versions`.

Se prefirió el segmento de ruta a una cabecera porque hace la versión visible en cualquier registro,
enlace o captura de pantalla, sin herramientas adicionales. Una versión no declarada no resuelve
ningún endpoint.

## 5. Errores

### Formato

Todas las respuestas de error son `ProblemDetails` (RFC 7807) con dos extensiones propias:

| Campo | Contenido |
| ----- | ----- |
| `codigoError` | Código estable de `CodigosError`. Los clientes reaccionan a esto, no al texto. |
| `identificadorCorrelacion` | `TraceIdentifier` de la petición, presente también en el registro del servidor. |
| `campo` | Propiedad asociada, cuando el error corresponde a una. |

### Lo que nunca sale

`ManejadorExcepcionesGlobal` implementa `IExceptionHandler`: registra la excepción completa del lado
del servidor y devuelve al cliente un `500` con un mensaje genérico y el identificador de
correlación. **Ni trazas, ni rutas del sistema de archivos, ni consultas SQL, ni nombres de tabla o
de restricción, ni cadenas de conexión.**

Esta regla se verifica de forma automática:
`ProveedoresApiTests.RespuestasDeError_NuncaExponenTrazasNiRutasInternas` comprueba que el cuerpo de
una respuesta de error no contiene «Licitaciones.Infrastructure», «Npgsql», «StackTrace» ni rutas de
Windows.

### Validación del modelo

`InvalidModelStateResponseFactory` está personalizada para que los errores de anotaciones tengan el
mismo formato que los de negocio: `ValidationProblemDetails` con `codigoError` e
`identificadorCorrelacion`, más `errors` con el detalle por campo. Sin esa personalización, un
cliente tendría que manejar dos formatos de error distintos.

## 6. Documentación OpenAPI

Swashbuckle genera el documento desde los controladores e **incorpora los comentarios XML** de los
ensamblados de API y de aplicación:

```csharp
opciones.IncludeXmlComments(ruta, includeControllerXmlComments: true);
```

Por eso `GenerateDocumentationFile` está activo en toda la solución. La consecuencia práctica: el
contrato publicado y los comentarios del código no pueden divergir, porque son el mismo texto.

Los atributos `ProducesResponseType` declaran todos los códigos posibles de cada endpoint, de modo
que la documentación indica no solo el caso feliz sino también cuándo esperar `404`, `409` o `422`.

## 7. Enumeraciones como texto

`JsonStringEnumConverter` hace que los estados viajen como `"Publicada"` y no como `1`. El contrato
queda legible y no se rompe si cambian los valores numéricos internos de la enumeración.

## 8. Paginación

Todos los listados devuelven `PaginaResultado<T>` con elementos y metadatos. `TamanoPagina` se limita
a 100: sin ese tope, una petición podría pedir la tabla completa y agotar la memoria del proceso.

## 9. Sondas de salud

| Ruta | Comprueba | Usada por |
| ----- | ----- | ----- |
| `/salud/vivo` | Que el proceso responde. `Predicate = _ => false` excluye todas las comprobaciones registradas. | `livenessProbe` y `startupProbe`. |
| `/salud/listo` | Además, la base de datos. | `readinessProbe` y el `HEALTHCHECK` de Docker. |

La separación es lo que evita que una caída de PostgreSQL reinicie en bucle todos los pods de la
aplicación. Ver [../kubernetes.md](../kubernetes.md) §4.

## 10. Pruebas

`ProveedoresApiTests`, `LicitacionesApiTests`, `OfertasApiTests` y `ArranqueAplicacionTests`
ejercitan el mismo `Program` que se publica en el contenedor, contra PostgreSQL real: enrutado,
versionado, filtros, validación de modelo y traducción de errores. Ver [../pruebas.md](../pruebas.md)
§4.

---

[← Volver al índice de documentación](../README.md)
