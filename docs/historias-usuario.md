# Historias de usuario

Las historias están escritas desde la perspectiva del cliente (analista de compras). Cada una tiene
prioridad, estimación en puntos de historia, criterios de aceptación verificables y la evidencia que
demuestra su cumplimiento.

## Escala de estimación

Se usa una serie reducida, adecuada para un equipo pequeño: **1, 2, 3, 5, 8** puntos. Un punto equivale
aproximadamente al esfuerzo de la historia más simple del proyecto (H-01).

## Convención de evidencia

| Tipo | Significado |
| ----- | ----- |
| **Prueba** | Clase o método de prueba automatizada que verifica el criterio. |
| **Módulo** | Documento de `/docs/modulos` que describe el comportamiento implementado. |

---

## Iteración 1 — Fundación, dominio y proveedores

### H-01 · Registrar un proveedor

> Como analista de compras quiero registrar un proveedor con un nombre válido para poder asociarle ofertas.

| Atributo | Valor |
| ----- | ----- |
| Prioridad | Alta |
| Estimación | 2 |
| Iteración | 1 |

**Criterios de aceptación**

1. El nombre es obligatorio y admite solo letras, números, espacios, punto, coma y paréntesis.
2. El nombre se guarda con los espacios laterales recortados y los espacios internos repetidos colapsados.
3. Al guardar se genera automáticamente un identificador que la persona usuaria no puede editar.
4. Un nombre con caracteres no permitidos (por ejemplo `Empresa #1`) se rechaza con un mensaje junto al campo.

**Evidencia** · Pruebas: `ProveedorTests`, `ServicioProveedoresTests` · Módulo: [proveedores.md](modulos/proveedores.md)

---

### H-02 · Impedir proveedores duplicados

> Como analista de compras quiero que el sistema rechace nombres de proveedor repetidos para evitar
> registros duplicados que distorsionen las comparaciones.

| Atributo | Valor |
| ----- | ----- |
| Prioridad | Alta |
| Estimación | 3 |
| Iteración | 1 |

**Criterios de aceptación**

1. `Empresa Central`, `empresa central` y `EMPRESA   CENTRAL` se consideran el mismo proveedor.
2. El rechazo ocurre en la interfaz, en el servidor y en PostgreSQL mediante un índice único.
3. El mensaje indica claramente que el proveedor ya existe.
4. Si un proveedor fue eliminado lógicamente, su nombre queda disponible nuevamente.

**Evidencia** · Pruebas: `NormalizadorTextoTests`, `ServicioProveedoresTests`, `PersistenciaProveedoresTests`
· Módulo: [proveedores.md](modulos/proveedores.md)

---

### H-03 · Consultar y editar proveedores

> Como analista de compras quiero listar, buscar y editar proveedores para mantener el padrón al día.

| Atributo | Valor |
| ----- | ----- |
| Prioridad | Alta |
| Estimación | 2 |
| Iteración | 1 |

**Criterios de aceptación**

1. El listado admite paginación, búsqueda por nombre y ordenamiento.
2. Al editar, la unicidad se valida excluyendo el propio registro.
3. El listado muestra cuántas ofertas tiene cada proveedor.

**Evidencia** · Pruebas: `ServicioProveedoresTests`, `ProveedoresApiTests` · Módulo: [proveedores.md](modulos/proveedores.md)

---

### H-04 · Eliminar un proveedor conservando su historial

> Como analista de compras quiero eliminar un proveedor sin perder las ofertas que ya presentó, para no
> alterar la evidencia de procesos anteriores.

| Atributo | Valor |
| ----- | ----- |
| Prioridad | Media |
| Estimación | 2 |
| Iteración | 1 |

**Criterios de aceptación**

1. La eliminación es lógica: se marca `DeletedAt` y el proveedor desaparece de los listados ordinarios.
2. Las ofertas asociadas se conservan.
3. Un proveedor eliminado no puede presentar nuevas ofertas.
4. La interfaz solicita confirmación antes de eliminar.

**Evidencia** · Pruebas: `ProveedorTests`, `ServicioProveedoresTests` · Módulo: [proveedores.md](modulos/proveedores.md)

---

### H-05 · Persistir la información en PostgreSQL

> Como responsable técnico quiero que los datos se guarden en PostgreSQL con restricciones reales para que
> la integridad no dependa únicamente del código de la aplicación.

| Atributo | Valor |
| ----- | ----- |
| Prioridad | Alta |
| Estimación | 5 |
| Iteración | 1 |

**Criterios de aceptación**

1. Existen migraciones versionadas y datos semilla de estados, niveles de aprobación y tipo de cambio inicial.
2. Los montos usan `numeric(18,2)`; no se emplea `float` ni `double`.
3. Existen claves foráneas, índices únicos y restricciones `CHECK`.
4. La cadena de conexión proviene de variables de entorno; el repositorio no contiene credenciales reales.

**Evidencia** · Pruebas: `EsquemaBaseDatosTests`, `PersistenciaProveedoresTests` · Módulo: [persistencia.md](modulos/persistencia.md)

---

## Iteración 2 — Licitaciones y ofertas

### H-06 · Crear una licitación

> Como analista de compras quiero crear una licitación con código único, presupuesto y fecha de cierre
> seleccionada en un calendario para iniciar un proceso de compra.

| Atributo | Valor |
| ----- | ----- |
| Prioridad | Alta |
| Estimación | 3 |
| Iteración | 2 |

**Criterios de aceptación**

1. El código es único ignorando espacios laterales y mayúsculas.
2. El presupuesto debe ser mayor que cero.
3. La fecha y hora de cierre se elige con un control de calendario y debe ser futura.
4. La licitación nace en estado `Borrador`.

**Evidencia** · Pruebas: `LicitacionTests`, `ServicioLicitacionesTests` · Módulo: [licitaciones.md](modulos/licitaciones.md)

---

### H-07 · Controlar el ciclo de estados

> Como analista de compras quiero que solo se permitan las transiciones válidas para que ningún proceso
> cerrado vuelva a abrirse.

| Atributo | Valor |
| ----- | ----- |
| Prioridad | Alta |
| Estimación | 3 |
| Iteración | 2 |

**Criterios de aceptación**

1. `Borrador → Publicada` exige título, presupuesto mayor que cero y fecha de cierre futura.
2. `Borrador → Cerrada` se permite como cancelación documentada.
3. `Publicada → Cerrada` se permite.
4. `Publicada → Borrador` y cualquier transición desde `Cerrada` se rechazan.

**Evidencia** · Pruebas: `TransicionesLicitacionTests`, `LicitacionTests` · Módulo: [licitaciones.md](modulos/licitaciones.md)

---

### H-08 · Registrar una oferta válida

> Como analista de compras quiero registrar la oferta de un proveedor para una licitación publicada, para
> poder compararla con las demás.

| Atributo | Valor |
| ----- | ----- |
| Prioridad | Alta |
| Estimación | 3 |
| Iteración | 2 |

**Criterios de aceptación**

1. El monto debe ser mayor que cero.
2. La oferta no puede superar el presupuesto; una oferta igual al presupuesto es válida.
3. Solo se admiten ofertas en licitaciones publicadas y vigentes.

**Evidencia** · Pruebas: `OfertaTests`, `ServicioOfertasTests` · Módulo: [ofertas.md](modulos/ofertas.md)

---

### H-09 · Rechazar ofertas inválidas

> Como analista de compras quiero que el sistema rechace ofertas duplicadas, fuera de presupuesto o
> vencidas, para garantizar la validez del proceso.

| Atributo | Valor |
| ----- | ----- |
| Prioridad | Alta |
| Estimación | 3 |
| Iteración | 2 |

**Criterios de aceptación**

1. Un proveedor no puede registrar dos ofertas en la misma licitación; existe un índice único compuesto.
2. Una oferta superior al presupuesto se rechaza con mensaje explicativo.
3. Cuando la fecha y hora actual es igual o posterior a la fecha de cierre, no se acepta ninguna oferta.
4. Las ofertas de licitaciones cerradas no se pueden crear, editar ni eliminar.

**Evidencia** · Pruebas: `OfertaTests`, `ServicioOfertasTests`, `PersistenciaOfertasTests` · Módulo: [ofertas.md](modulos/ofertas.md)

---

### H-10 · Proteger el presupuesto frente a ofertas existentes

> Como analista de compras quiero que no se pueda bajar el presupuesto por debajo de una oferta ya
> registrada, para que las ofertas recibidas no queden fuera de rango.

| Atributo | Valor |
| ----- | ----- |
| Prioridad | Media |
| Estimación | 2 |
| Iteración | 2 |

**Criterios de aceptación**

1. Al editar una licitación, el nuevo presupuesto debe ser mayor o igual a la oferta más alta registrada.
2. El mensaje indica el monto que impide la reducción.

**Evidencia** · Pruebas: `LicitacionTests`, `ServicioLicitacionesTests` · Módulo: [licitaciones.md](modulos/licitaciones.md)

---

### H-11 · Determinar la mejor oferta y su clasificación

> Como analista de compras quiero conocer la mejor oferta, el ahorro obtenido y su clasificación para
> sustentar la decisión de compra.

| Atributo | Valor |
| ----- | ----- |
| Prioridad | Alta |
| Estimación | 3 |
| Iteración | 2 |

**Criterios de aceptación**

1. La mejor oferta es la de menor monto en CRC; en empate gana la registrada primero.
2. Sin ofertas se muestra «Sin ofertas válidas».
3. Ahorro ≥ 10 % → «Oferta conveniente»; 0 % < ahorro < 10 % → «Oferta aceptable»; oferta igual al
   presupuesto → «Oferta válida sin ahorro».

**Evidencia** · Pruebas: `EvaluadorOfertasTests`, `ServicioLicitacionesTests` · Módulo: [ofertas.md](modulos/ofertas.md)

---

## Iteración 3 — Aprobación, moneda, interfaz web y API REST

### H-12 · Obtener el aprobador desde una tabla parametrizable

> Como analista de compras quiero que el aprobador se determine según rangos configurables para poder
> cambiar los umbrales sin modificar el sistema.

| Atributo | Valor |
| ----- | ----- |
| Prioridad | Alta |
| Estimación | 3 |
| Iteración | 3 |

**Criterios de aceptación**

1. El aprobador se obtiene consultando la tabla, nunca con una cadena de condiciones `if/else`.
2. Los rangos no pueden traslaparse.
3. Solo puede existir un rango sin monto máximo.
4. Los rangos semilla son los del enunciado: Encargado de área, Gerencia y Junta Directiva.

**Evidencia** · Pruebas: `SelectorNivelAprobacionTests`, `ServicioNivelesAprobacionTests`
· Módulo: [niveles-aprobacion.md](modulos/niveles-aprobacion.md)

---

### H-13 · Administrar el tipo de cambio

> Como analista de compras quiero administrar el tipo de cambio localmente para que el sistema funcione sin
> depender de Internet.

| Atributo | Valor |
| ----- | ----- |
| Prioridad | Alta |
| Estimación | 3 |
| Iteración | 3 |

**Criterios de aceptación**

1. El tipo de cambio debe ser mayor que cero.
2. Solo puede haber un registro activo; activar uno desactiva los demás en una sola transacción.
3. El tipo de cambio activo no se puede eliminar.
4. Existe un tipo de cambio semilla activo desde la primera ejecución.

**Evidencia** · Pruebas: `ServicioTiposCambioTests`, `PersistenciaTiposCambioTests` · Módulo: [tipo-cambio.md](modulos/tipo-cambio.md)

---

### H-14 · Ver los montos en colones o en dólares

> Como analista de compras quiero alternar la visualización entre CRC y USD para comparar montos con
> contrapartes internacionales.

| Atributo | Valor |
| ----- | ----- |
| Prioridad | Alta |
| Estimación | 3 |
| Iteración | 3 |

**Criterios de aceptación**

1. Un botón visible alterna toda la interfaz entre CRC y USD.
2. La conversión usa `Monto CRC / Tipo de cambio` y no altera los valores almacenados.
3. Se muestra la fecha del tipo de cambio utilizado.
4. La preferencia se conserva al navegar entre páginas.

**Evidencia** · Pruebas: `ConversorMonedaTests`, `ServicioConversionMonedaTests`, `NavegacionE2ETests`
· Módulo: [tipo-cambio.md](modulos/tipo-cambio.md)

---

### H-15 · Navegar por una interfaz clara y adaptable

> Como analista de compras quiero una landing page explicativa y una navegación consistente para entender y
> usar el sistema sin capacitación previa.

| Atributo | Valor |
| ----- | ----- |
| Prioridad | Alta |
| Estimación | 5 |
| Iteración | 3 |

**Criterios de aceptación**

1. La landing page explica el flujo de licitación, ofertas, mejor oferta, aprobación y conversión monetaria.
2. El menú da acceso a Inicio, Licitaciones, Proveedores, Ofertas, Niveles de aprobación, Tipo de cambio y
   la documentación interactiva de la API.
3. El diseño es adaptable a computadora y móvil.
4. Los recursos de interfaz se sirven localmente: la aplicación no queda inutilizable sin CDN.

**Evidencia** · Pruebas: `NavegacionE2ETests` · Módulo: [interfaz-web.md](modulos/interfaz-web.md)

---

### H-16 · Alternar entre modo claro y modo oscuro

> Como analista de compras quiero elegir el tema visual y que se recuerde mi preferencia para trabajar
> cómodamente.

| Atributo | Valor |
| ----- | ----- |
| Prioridad | Media |
| Estimación | 2 |
| Iteración | 3 |

**Criterios de aceptación**

1. Existe un control visible para cambiar de tema.
2. La preferencia persiste entre páginas y entre sesiones.
3. El tema inicial respeta la preferencia del sistema operativo.

**Evidencia** · Pruebas: `NavegacionE2ETests` · Módulo: [interfaz-web.md](modulos/interfaz-web.md)

---

### H-17 · Operar el sistema mediante una API REST

> Como sistema integrado quiero consumir una API REST versionada y documentada para automatizar las
> operaciones sin usar la interfaz web.

| Atributo | Valor |
| ----- | ----- |
| Prioridad | Alta |
| Estimación | 8 |
| Iteración | 3 |

**Criterios de aceptación**

1. Los endpoints están bajo `/api/v1` y se documentan con OpenAPI/Swagger.
2. Se usan DTO; nunca se exponen las entidades de Entity Framework Core.
3. Los códigos HTTP son correctos: 200, 201, 204, 400, 404, 409, 422 y 500 controlado.
4. Los errores se devuelven como `ProblemDetails` con título, estado, detalle seguro, código de error e
   identificador de correlación, sin trazas ni rutas internas.
5. Los listados admiten paginación, filtrado y ordenamiento.

**Evidencia** · Pruebas: `ProveedoresApiTests`, `LicitacionesApiTests`, `OfertasApiTests` · Módulo: [api-rest.md](modulos/api-rest.md)

---

## Iteración 4 — Verificación, empaquetado y despliegue

### H-18 · Verificar el sistema con pruebas automatizadas

> Como responsable técnico quiero una batería de pruebas unitarias, de integración y funcionales para poder
> refactorizar con confianza.

| Atributo | Valor |
| ----- | ----- |
| Prioridad | Alta |
| Estimación | 8 |
| Iteración | 4 |

**Criterios de aceptación**

1. Las pruebas unitarias cubren todas las reglas de negocio del enunciado.
2. Las pruebas de integración se ejecutan contra PostgreSQL real en contenedor, nunca contra SQLite.
3. Las pruebas funcionales recorren el flujo completo desde el navegador.
4. La cobertura de Domain y Application alcanza al menos 80 %, y la del proyecto completo al menos 70 %.

**Evidencia** · Documento: [pruebas.md](pruebas.md)

---

### H-19 · Ejecutar la solución con Docker Compose

> Como responsable técnico quiero levantar aplicación y base de datos con un solo comando para reproducir el
> entorno sin instalación manual.

| Atributo | Valor |
| ----- | ----- |
| Prioridad | Alta |
| Estimación | 3 |
| Iteración | 4 |

**Criterios de aceptación**

1. `docker compose up --build` levanta la solución sin pasos manuales complejos.
2. La imagen es multi-etapa y la aplicación corre con un usuario no privilegiado.
3. Existen volumen persistente, variables de entorno y *health checks*.
4. Los datos sobreviven al reinicio de los contenedores.

**Evidencia** · Documento: [docker.md](docker.md)

---

### H-20 · Desplegar en Kubernetes

> Como responsable técnico quiero manifiestos de Kubernetes completos para desplegar la solución en un
> clúster con persistencia y configuración segura.

| Atributo | Valor |
| ----- | ----- |
| Prioridad | Alta |
| Estimación | 5 |
| Iteración | 4 |

**Criterios de aceptación**

1. Existen `Deployment`, `Service`, `StatefulSet` de PostgreSQL, `PersistentVolumeClaim`, `ConfigMap` y
   `Secret` de ejemplo.
2. Se definen sondas de arranque, disponibilidad y actividad, con solicitudes y límites de recursos.
3. Las migraciones se ejecutan de forma controlada, sin que varias réplicas las apliquen a la vez.
4. Los datos sobreviven al reinicio de los pods.

**Evidencia** · Documento: [kubernetes.md](kubernetes.md)

---

### H-21 · Mantener la integración continua en verde

> Como responsable técnico quiero que cada cambio se compile, pase pruebas y análisis automáticamente para
> impedir que se integre trabajo defectuoso.

| Atributo | Valor |
| ----- | ----- |
| Prioridad | Alta |
| Estimación | 3 |
| Iteración | 4 |

**Criterios de aceptación**

1. El flujo restaura dependencias, compila, ejecuta pruebas y publica cobertura.
2. Verifica el formato del código y el análisis estático.
3. Construye la imagen Docker y valida los manifiestos de Kubernetes.
4. Revisa dependencias vulnerables y bloquea la integración cuando algo falla.

**Evidencia** · Archivo: `.github/workflows/ci.yml`

---

### H-22 · Documentar el sistema dentro del repositorio

> Como persona evaluadora quiero encontrar toda la documentación en `/docs`, en Markdown, para revisar el
> proyecto sin archivos externos.

| Atributo | Valor |
| ----- | ----- |
| Prioridad | Alta |
| Estimación | 5 |
| Iteración | 4 |

**Criterios de aceptación**

1. `/docs/README.md` funciona como índice navegable.
2. Cada módulo tiene su documento con propósito, responsabilidades, dependencias, entradas, salidas, reglas,
   errores y pruebas.
3. `integracion-modulos.md` explica los flujos de extremo a extremo.
4. Arquitectura y modelo de datos incluyen diagramas Mermaid.

**Evidencia** · Documento: [README.md](README.md)

---

## Resumen de puntos por iteración

| Iteración | Historias | Puntos planificados |
| ----- | ----- | ----- |
| 1 | H-01 … H-05 | 14 |
| 2 | H-06 … H-11 | 17 |
| 3 | H-12 … H-17 | 24 |
| 4 | H-18 … H-22 | 24 |
| **Total** | **22 historias** | **79** |

La velocidad observada por iteración y su comparación con lo planificado se registran en
[bitacora-xp.md](bitacora-xp.md).

---

[← Volver al índice de documentación](README.md)
