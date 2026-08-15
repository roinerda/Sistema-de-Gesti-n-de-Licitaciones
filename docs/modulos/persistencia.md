# Módulo: Persistencia

Historias cubiertas: H-02, H-19. Código principal: `src/Licitaciones.Infrastructure/Persistencia/`.

El esquema físico, con sus tablas, índices y restricciones, está en
[../modelo-datos.md](../modelo-datos.md). Este documento explica **cómo se implementa el acceso a
datos**.

## 1. PostgreSQL, sin sustitutos

El enunciado prohíbe reemplazar PostgreSQL por SQLite, ni en la aplicación ni en las pruebas de
integración. Las pruebas de este proyecto explican por qué: verifican índices únicos **parciales**,
restricciones CHECK con funciones (`length(btrim(...))`) y traducción de códigos de error del motor.
Ninguna de esas cosas existe igual en un sustituto en memoria, y una suite que pasara contra SQLite
no diría nada sobre lo que ocurre en producción.

Las pruebas de integración usan Testcontainers con `postgres:16-alpine`, la misma imagen que se
despliega.

## 2. Configuración del modelo

Una clase `IEntityTypeConfiguration<T>` por entidad, descubiertas con
`ApplyConfigurationsFromAssembly`. Nada de anotaciones en las entidades: el dominio no debe conocer
el ORM.

Cada configuración declara de forma explícita el nombre de tabla, el de cada columna, la precisión,
las restricciones y los índices. Confiar en las convenciones habría producido `PascalCase` en la base
de datos y habría dejado el nombre de cada restricción a merced de la versión de EF Core, con lo que
un mensaje de error controlado dependería de un detalle inestable.

## 3. Repositorios

Uno por agregado, implementando los puertos que define la capa de aplicación. Tres decisiones
recurrentes:

**`AsNoTracking` en las consultas de solo lectura.** Un listado no necesita seguimiento de cambios;
activarlo cuesta memoria y tiempo por cada entidad materializada.

**`Include` explícito donde el DTO lo necesita.** El listado de ofertas muestra el código de la
licitación y el nombre del proveedor, así que los carga en la misma consulta. Sin eso, cada fila
dispararía una consulta adicional.

**Conteos por lote.** Los listados muestran cuántas ofertas tiene cada fila.
`ContarOfertasAsync(IReadOnlyCollection<Guid>)` resuelve la página completa con un solo `GROUP BY`,
en lugar de una consulta por fila.

### Búsqueda sin romper el índice

```csharp
consulta.Where(p => EF.Functions.ILike(p.NombreNormalizado, PatronesBusqueda.ParaContiene(texto)))
```

`ILike` es el operador insensible a mayúsculas de PostgreSQL. `ParaContiene` escapa `\`, `%` y `_`
para que un texto que los contenga no se interprete como comodín.

No se usa `ToUpper()` dentro de la consulta por dos razones: los analizadores lo marcan como
dependiente de la cultura, y —más importante— una función aplicada a la columna impide usar el
índice y obliga a recorrer la tabla entera.

## 4. Unidad de trabajo

`UnidadDeTrabajo` centraliza la confirmación y **traduce los errores del motor**:

| Estado SQL | Significado | Excepción de aplicación |
| ----- | ----- | ----- |
| `23505` | Violación de índice único | `ViolacionUnicidadException` (con el nombre del índice) |
| `23503` | Violación de clave foránea | `ViolacionIntegridadException` |
| `23514` | Violación de restricción CHECK | `ViolacionIntegridadException` |
| — | `DbUpdateConcurrencyException` | `ConflictoConcurrenciaException` |

`ProtectorCasoUso`, en la capa de aplicación, convierte esas excepciones en `Resultado` con su
código de error. El dominio y los casos de uso nunca mencionan PostgreSQL, y el mensaje que llega al
cliente no contiene el código SQL, el nombre de la restricción ni la consulta.

`PersistenciaOfertasTests.EliminarUnProveedorConOfertas_SeTraduceAErrorDeIntegridadControlado`
comprueba exactamente eso.

## 5. Transacciones

`EjecutarEnTransaccionAsync` envuelve las operaciones que tocan varios registros, usando la
estrategia de ejecución de EF Core para que un reintento por error transitorio repita la transacción
completa y no un fragmento.

Se usa en la activación del tipo de cambio, donde hay que desactivar el anterior y activar el nuevo
sin que exista un instante con dos activos. Ver [tipo-cambio.md](tipo-cambio.md) §3.

## 6. Concurrencia optimista

Cada entidad lleva `version`, un entero declarado como testigo de concurrencia:

```csharp
builder.Property(e => e.Version)
       .HasColumnName("version")
       .IsConcurrencyToken()
       .IsRequired();
```

`EntidadBase.RegistrarActualizacion` lo incrementa en cada modificación. Los DTO de lectura lo
exponen y los de escritura lo aceptan, de modo que el formulario o el cuerpo de la petición devuelven
la versión que la persona tenía a la vista.

`IUnidadDeTrabajo.EstablecerVersionOriginal` la fija antes de guardar:

```csharp
_contexto.Entry(entidad).Property(e => e.Version).OriginalValue = version;
```

**Sin esa línea**, la comparación usaría la versión leída milisegundos antes de escribir y solo
detectaría choques simultáneos. Con ella se detecta el caso real: dos personas abren el mismo
formulario, la primera guarda y la segunda intenta guardar sobre cambios que nunca vio. Lo verifica
`ConcurrenciaOptimistaTests.GuardarConUnaVersionVieja_SeRechazaAunqueLaEdicionSeaPosterior`.

### Por qué no `xmin`

PostgreSQL ofrece `xmin` como testigo natural. Se descartó por dos motivos: la migración que lo
declara como columna real es rechazada por el motor, porque es una columna de sistema; y su valor no
puede viajar cómodamente en un DTO ni en un campo oculto de formulario, que es justo lo que hace
falta para detectar la edición concurrente entre peticiones distintas.

## 7. Auditoría

`CreatedAt` y `UpdatedAt` en todas las entidades, `DeletedAt` en las que admiten borrado lógico.
Siempre en UTC.

Los métodos de dominio ya actualizan las marcas; `LicitacionesDbContext.ActualizarMarcasDeAuditoria`
las repite en `SaveChanges` como red de seguridad, por si alguna modificación llegara por un camino
que no pase por ellos, y protege `CreatedAt` de ser sobrescrito.

## 8. Migraciones

Una sola migración, `EsquemaInicial`, versionada en el repositorio junto al modelo instantáneo.
`InicializadorBaseDatos` las aplica al arrancar si la configuración lo autoriza, con dos
salvaguardas:

**Espera activa.** Hasta diez intentos de conexión con tres segundos de separación. En un contenedor
o en un clúster, la aplicación puede arrancar antes de que el motor termine de inicializar.

**Bloqueo de aviso.** `pg_advisory_lock(728314905)` sobre una conexión propia, antes de migrar. Con
varias réplicas, solo una migra y las demás esperan; cuando obtienen el bloqueo ya no hay migraciones
pendientes. El bloqueo va en una conexión separada de la del contexto para no interferir con las
transacciones de la propia migración.

`FabricaDbContextTiempoDiseno` permite ejecutar `dotnet ef` sin levantar la aplicación, leyendo la
cadena de conexión de la variable de entorno.

## 9. Configuración y secretos

La cadena de conexión se lee de `ConnectionStrings:Licitaciones`, que en contenedores llega como
`ConnectionStrings__Licitaciones`. En `appsettings.json` está vacía a propósito y
`ResolverCadenaConexion` lanza si no hay ninguna: es preferible que la aplicación no arranque a que
arranque apuntando a un lugar equivocado.

`EnableRetryOnFailure` reintenta ante errores transitorios, útil mientras el contenedor de PostgreSQL
todavía está iniciando o durante un reinicio del pod.

## 10. Pruebas

Ver [../pruebas.md](../pruebas.md) §4. Contra PostgreSQL real se verifican el esquema completo, los
índices únicos parciales con su filtro, las restricciones CHECK, la traducción de errores de
integridad, la unicidad de la fila activa del tipo de cambio y las tres formas de conflicto de
concurrencia.

---

[← Volver al índice de documentación](../README.md)
