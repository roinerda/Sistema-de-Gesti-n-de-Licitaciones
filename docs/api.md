# API REST

Base: `/api/v1`. Formato: JSON. Documentación interactiva en `/swagger`; documento OpenAPI en
`/swagger/v1/swagger.json`.

La API **nunca expone entidades de Entity Framework Core**: todo entra y sale como DTO. Eso permite
cambiar el modelo persistente sin romper a los clientes, y evita que una navegación cargue por
accidente medio grafo en una respuesta.

## 1. Versionado

La versión viaja en la ruta (`/api/v1/...`), leída con `UrlSegmentApiVersionReader`. La respuesta
incluye la cabecera `api-supported-versions`. Una versión no declarada (`/api/v9/...`) devuelve un
error de versión no soportada, no un 404 genérico.

Se eligió el segmento de ruta y no una cabecera porque hace la versión visible en cualquier registro,
enlace o captura de pantalla, sin herramientas adicionales.

## 2. Códigos de estado

| Código | Cuándo |
| ----- | ----- |
| `200 OK` | Consulta o actualización correcta. |
| `201 Created` | Recurso creado. Incluye `Location` apuntando al recurso. |
| `204 No Content` | Eliminación correcta. |
| `400 Bad Request` | El cuerpo o la ruta no cumplen el contrato: falta un campo obligatorio, un número está fuera de rango, un identificador no es un GUID. |
| `404 Not Found` | El recurso no existe o fue dado de baja. |
| `409 Conflict` | Choque con el estado actual: duplicado, dependencias que impiden borrar, o conflicto de concurrencia. |
| `422 Unprocessable Entity` | La petición es sintácticamente válida pero una regla de negocio la rechaza. |
| `500 Internal Server Error` | Fallo no previsto. La respuesta solo lleva el identificador de correlación. |

La distinción entre `400` y `422` es deliberada. `400` significa «esto ni siquiera se entiende como
una petición válida»; `422` significa «se entiende perfectamente, pero el negocio no lo permite». Un
cliente puede reintentar el segundo con otros datos sin cambiar su código.

## 3. Formato de error

Todas las respuestas de error son `ProblemDetails` (RFC 7807):

```json
{
  "title": "Regla de negocio no cumplida",
  "status": 422,
  "detail": "El monto ofertado no puede superar el presupuesto estimado de la licitación.",
  "instance": "/api/v1/licitaciones/6f1f4d3e-.../ofertas",
  "codigoError": "OFERTA_SUPERA_PRESUPUESTO",
  "identificadorCorrelacion": "0HN7GQ2K8V1P3:00000001",
  "campo": "MontoOfertadoCrc"
}
```

- `codigoError` es estable: los clientes reaccionan a él, no al texto, que puede cambiar.
- `identificadorCorrelacion` es el `TraceIdentifier` de la petición y aparece también en el registro
  del servidor, de modo que un reporte de error se puede localizar sin adivinar.
- `campo` aparece cuando el error se asocia a una propiedad concreta, y es lo que la interfaz web usa
  para colocar el mensaje junto al control correspondiente.

**Lo que nunca aparece:** trazas de excepción, rutas del sistema de archivos, consultas SQL, nombres
de tabla o de restricción, cadenas de conexión ni mensajes del proveedor de datos. El manejador
global registra el detalle completo del lado del servidor y devuelve al cliente solo un mensaje
genérico con su identificador de correlación.

Los errores de validación del modelo añaden `errors` con la lista por campo, manteniendo el mismo
`codigoError` (`VALIDACION_ENTRADA`) y el mismo identificador de correlación.

## 4. Paginación, filtrado y ordenamiento

Parámetros comunes de todos los listados:

| Parámetro | Tipo | Valor por omisión | Notas |
| ----- | ----- | ----- | ----- |
| `Pagina` | entero | 1 | Se ajusta a 1 si llega menor. |
| `TamanoPagina` | entero | 20 | Se limita a un máximo de 100, para que nadie pueda pedir la tabla entera. |
| `Buscar` | texto | — | Búsqueda insensible a mayúsculas y a espacios repetidos. |
| `OrdenarPor` | texto | según el recurso | Valores admitidos indicados en cada endpoint. |
| `Descendente` | booleano | `false` | Invierte el orden. |

Respuesta:

```json
{
  "elementos": [ ... ],
  "pagina": 1,
  "tamanoPagina": 20,
  "totalElementos": 37,
  "totalPaginas": 2,
  "tienePaginaAnterior": false,
  "tienePaginaSiguiente": true
}
```

## 5. Endpoints

### 5.1 Proveedores

| Método | Ruta | Descripción |
| ----- | ----- | ----- |
| `GET` | `/api/v1/proveedores` | Lista. Filtros extra: `IncluirEliminados`. Orden: `nombre`, `fecha`. |
| `GET` | `/api/v1/proveedores/{id}` | Detalle. |
| `GET` | `/api/v1/proveedores/{id}/ofertas` | Ofertas presentadas por el proveedor. |
| `POST` | `/api/v1/proveedores` | Crea. `409` si el nombre normalizado ya existe. |
| `PUT` | `/api/v1/proveedores/{id}` | Actualiza. Envíe `version` para detectar ediciones concurrentes. |
| `DELETE` | `/api/v1/proveedores/{id}` | Borrado lógico. `409` si tiene ofertas y la política lo impide. |

Cuerpo de creación y actualización:

```json
{ "nombre": "Constructora Alfa S.A.", "version": 3 }
```

### 5.2 Licitaciones

| Método | Ruta | Descripción |
| ----- | ----- | ----- |
| `GET` | `/api/v1/licitaciones` | Lista. Filtros extra: `Estado`, `IncluirEliminadas`. Orden: `codigo`, `titulo`, `cierre`, `presupuesto`. |
| `GET` | `/api/v1/licitaciones/{id}` | Detalle, con la evaluación de ofertas y las transiciones permitidas. |
| `POST` | `/api/v1/licitaciones` | Crea en estado `Borrador`. |
| `PUT` | `/api/v1/licitaciones/{id}` | Actualiza. `422` si el nuevo presupuesto queda por debajo de una oferta ya registrada. |
| `PATCH` | `/api/v1/licitaciones/{id}/estado` | Aplica una transición. `422` si no está permitida. |
| `DELETE` | `/api/v1/licitaciones/{id}` | Borrado lógico. `409` si tiene ofertas. |
| `GET` | `/api/v1/licitaciones/{id}/ofertas` | Ofertas de la licitación. |
| `POST` | `/api/v1/licitaciones/{id}/ofertas` | Registra una oferta en esa licitación. |
| `GET` | `/api/v1/licitaciones/{id}/mejor-oferta` | Mejor oferta, ahorro, clasificación y aprobador. |

Cuerpo de creación:

```json
{
  "codigo": "LIC-2026-001",
  "titulo": "Compra de equipo de cómputo",
  "fechaCierre": "2026-09-30T23:59:00-06:00",
  "presupuestoEstimadoCrc": 10000000.00
}
```

Cambio de estado:

```json
{ "nuevoEstado": "Publicada" }
```

Los estados viajan como texto (`"Borrador"`, `"Publicada"`, `"Cerrada"`), no como número: el contrato
queda legible y no se rompe si cambian los valores internos de la enumeración.

Respuesta de `mejor-oferta`:

```json
{
  "licitacionId": "…",
  "licitacionCodigo": "LIC-2026-001",
  "presupuestoEstimadoCrc": 10000000.00,
  "oferta": { "id": "…", "proveedorNombre": "Constructora Beta", "montoOfertadoCrc": 8000000.00, "…": "…" },
  "porcentajeAhorro": 20.00,
  "clasificacion": "OfertaConveniente",
  "clasificacionDescripcion": "Oferta conveniente",
  "aprobador": "Gerencia"
}
```

`clasificacionDescripcion` contiene exactamente uno de los cuatro textos que fija el enunciado:
`Sin ofertas válidas`, `Oferta conveniente`, `Oferta aceptable`, `Oferta válida sin ahorro`.

### 5.3 Ofertas

| Método | Ruta | Descripción |
| ----- | ----- | ----- |
| `GET` | `/api/v1/ofertas` | Lista. Filtros extra: `LicitacionId`, `ProveedorId`. Orden: `monto` (por omisión), `proveedor`, `licitacion`, `fecha`. |
| `GET` | `/api/v1/ofertas/{id}` | Detalle. |
| `POST` | `/api/v1/ofertas` | Registra indicando licitación y proveedor. |
| `PUT` | `/api/v1/ofertas/{id}` | Cambia el monto. |
| `DELETE` | `/api/v1/ofertas/{id}` | Elimina. |

```json
{
  "licitacionId": "…",
  "proveedorId": "…",
  "montoOfertadoCrc": 8000000.00
}
```

Rechazos propios de este recurso: `OFERTA_SUPERA_PRESUPUESTO` (422), `OFERTA_DUPLICADA` (409),
`OFERTA_LICITACION_NO_PUBLICADA` (422) y `OFERTA_VENCIDA` (422) cuando la fecha de cierre ya pasó.

### 5.4 Niveles de aprobación

| Método | Ruta | Descripción |
| ----- | ----- | ----- |
| `GET` | `/api/v1/niveles-aprobacion` | Lista, ordenada por monto mínimo. |
| `GET` | `/api/v1/niveles-aprobacion/{id}` | Detalle. |
| `GET` | `/api/v1/niveles-aprobacion/aprobador?montoCrc=5000000` | Aprobador correspondiente al monto. |
| `POST` | `/api/v1/niveles-aprobacion` | Crea. `422` si el rango se traslapa o si ya hay otro rango abierto. |
| `PUT` | `/api/v1/niveles-aprobacion/{id}` | Actualiza con las mismas validaciones. |
| `DELETE` | `/api/v1/niveles-aprobacion/{id}` | Elimina. |

```json
{ "montoMinimoCrc": 1000000.00, "montoMaximoCrc": 9999999.99, "aprobador": "Gerencia" }
```

Omitir `montoMaximoCrc` (o enviarlo como `null`) declara un rango abierto por arriba. Solo puede
existir uno.

### 5.5 Tipos de cambio

| Método | Ruta | Descripción |
| ----- | ----- | ----- |
| `GET` | `/api/v1/tipos-cambio` | Historial. |
| `GET` | `/api/v1/tipos-cambio/activo` | Tipo de cambio vigente. |
| `GET` | `/api/v1/tipos-cambio/conversion?montoCrc=1040000` | Convierte un monto a dólares. |
| `GET` | `/api/v1/tipos-cambio/{id}` | Detalle. |
| `POST` | `/api/v1/tipos-cambio` | Crea. Si llega con `activo: true`, desactiva el anterior en la misma transacción. |
| `PUT` | `/api/v1/tipos-cambio/{id}` | Actualiza. |
| `PATCH` | `/api/v1/tipos-cambio/{id}/activar` | Marca este como activo y desactiva los demás. |
| `DELETE` | `/api/v1/tipos-cambio/{id}` | Elimina. `422` si está activo. |

Respuesta de la conversión:

```json
{
  "montoCrc": 1040000.00,
  "montoUsd": 2000.00,
  "crcPorUsd": 520.0000,
  "fechaTipoCambio": "2026-08-11T06:00:00+00:00"
}
```

La conversión **siempre** devuelve el tipo aplicado y su fecha. Un monto en dólares sin esa
referencia no significa nada, porque el valor cambia con el tiempo.

### 5.6 Sondas de salud

| Ruta | Qué comprueba |
| ----- | ----- |
| `GET /salud/vivo` | Que el proceso responde. No toca la base de datos. |
| `GET /salud/listo` | Que además la base de datos está disponible. |

## 6. Colección reproducible de solicitudes

El archivo [`assets/coleccion-api.http`](assets/coleccion-api.http) contiene el recorrido completo,
listo para ejecutar con la extensión REST Client de Visual Studio Code, con Visual Studio o con
JetBrains Rider. Las solicitudes están encadenadas: cada una toma el identificador de la respuesta
anterior, así que basta ejecutarlas en orden.

El mismo recorrido, con `curl`, sobre un entorno recién levantado con `docker compose up --build`:

```bash
BASE=http://localhost:8080/api/v1

# 1. Registrar dos proveedores
ALFA=$(curl -s -X POST "$BASE/proveedores" -H 'Content-Type: application/json' \
  -d '{"nombre":"Constructora Alfa"}' | python3 -c 'import sys,json;print(json.load(sys.stdin)["id"])')
BETA=$(curl -s -X POST "$BASE/proveedores" -H 'Content-Type: application/json' \
  -d '{"nombre":"Constructora Beta"}' | python3 -c 'import sys,json;print(json.load(sys.stdin)["id"])')

# 2. Crear la licitación (nace en borrador)
LIC=$(curl -s -X POST "$BASE/licitaciones" -H 'Content-Type: application/json' \
  -d '{"codigo":"LIC-2026-001","titulo":"Compra de equipo de cómputo",
       "fechaCierre":"2026-12-31T23:59:00-06:00","presupuestoEstimadoCrc":10000000.00}' \
  | python3 -c 'import sys,json;print(json.load(sys.stdin)["id"])')

# 3. Publicarla
curl -s -X PATCH "$BASE/licitaciones/$LIC/estado" -H 'Content-Type: application/json' \
  -d '{"nuevoEstado":"Publicada"}' > /dev/null

# 4. Registrar dos ofertas
curl -s -X POST "$BASE/licitaciones/$LIC/ofertas" -H 'Content-Type: application/json' \
  -d "{\"proveedorId\":\"$ALFA\",\"montoOfertadoCrc\":9500000.00}" > /dev/null
curl -s -X POST "$BASE/licitaciones/$LIC/ofertas" -H 'Content-Type: application/json' \
  -d "{\"proveedorId\":\"$BETA\",\"montoOfertadoCrc\":8000000.00}" > /dev/null

# 5. Consultar la mejor oferta: 20 % de ahorro, «Oferta conveniente», aprobador «Gerencia»
curl -s "$BASE/licitaciones/$LIC/mejor-oferta"

# 6. Intentar una oferta que supera el presupuesto: 422 con OFERTA_SUPERA_PRESUPUESTO
curl -s -X POST "$BASE/licitaciones/$LIC/ofertas" -H 'Content-Type: application/json' \
  -d "{\"proveedorId\":\"$ALFA\",\"montoOfertadoCrc\":11000000.00}"

# 7. Intentar una transición prohibida: 422 con LICITACION_TRANSICION_NO_PERMITIDA
curl -s -X PATCH "$BASE/licitaciones/$LIC/estado" -H 'Content-Type: application/json' \
  -d '{"nuevoEstado":"Borrador"}'
```

## 7. Códigos de error

| Código | Situación |
| ----- | ----- |
| `RECURSO_NO_ENCONTRADO` | El identificador no corresponde a ningún registro vigente. |
| `VALIDACION_ENTRADA` | Falló la validación de anotaciones del DTO. |
| `CONFLICTO_CONCURRENCIA` | Otra persona modificó el registro entre la lectura y la escritura. |
| `PROVEEDOR_NOMBRE_REQUERIDO` / `_DEMASIADO_LARGO` / `_CARACTERES_NO_PERMITIDOS` / `_DUPLICADO` | Reglas del nombre de proveedor. |
| `PROVEEDOR_CON_OFERTAS`, `PROVEEDOR_ELIMINADO` | Dependencias o estado del proveedor. |
| `LICITACION_CODIGO_REQUERIDO` / `_DEMASIADO_LARGO` / `_DUPLICADO` | Reglas del código. |
| `LICITACION_TITULO_REQUERIDO` / `_DEMASIADO_LARGO` | Reglas del título. |
| `LICITACION_PRESUPUESTO_INVALIDO` | El presupuesto no es mayor que cero. |
| `LICITACION_PRESUPUESTO_MENOR_A_OFERTA_EXISTENTE` | Bajar el presupuesto invalidaría una oferta ya registrada. |
| `LICITACION_FECHA_CIERRE_INVALIDA` | La fecha de cierre no es futura. |
| `LICITACION_TRANSICION_NO_PERMITIDA` | La transición no está en la tabla de transiciones. |
| `LICITACION_CERRADA`, `LICITACION_ELIMINADA`, `LICITACION_CON_OFERTAS` | Estado o dependencias de la licitación. |
| `OFERTA_MONTO_INVALIDO`, `OFERTA_SUPERA_PRESUPUESTO`, `OFERTA_DUPLICADA` | Reglas de la oferta. |
| `OFERTA_LICITACION_NO_PUBLICADA`, `OFERTA_VENCIDA` | La licitación no admite ofertas en este momento. |
| `NIVEL_APROBACION_RANGO_INVALIDO` / `_TRASLAPADO` / `_ABIERTO_DUPLICADO` | Reglas de los rangos de aprobación. |
| `NIVEL_APROBACION_APROBADOR_REQUERIDO` | Falta el nombre del aprobador. |
| `TIPO_CAMBIO_INVALIDO`, `TIPO_CAMBIO_ACTIVO_REQUERIDO`, `TIPO_CAMBIO_ACTIVO_NO_ELIMINABLE` | Reglas del tipo de cambio. |
