# Bitácora XP

Registro de lo que ocurrió realmente en cada iteración: qué se completó, qué pruebas lo respaldan,
qué se refactorizó y qué se aprendió. Esta bitácora describe hechos verificables contra el historial
del repositorio, no un relato ideal.

## Nota previa sobre honestidad del registro

Dos aclaraciones que conviene hacer por delante:

1. **Las fechas de los commits son las reales.** No se retrocedieron marcas de tiempo para simular un
   ritmo diario. El trabajo se concentró en dos jornadas largas (11 y 15 de agosto de 2026), y el
   historial lo refleja. El «ritmo sostenible» de XP se cumplió dentro de cada jornada en el sentido
   que importa aquí: se integró de forma incremental, con la batería de pruebas en verde en cada
   entrega, en lugar de acumular todo en un único commit final.
2. **El TDD fue estricto en el dominio y en los casos de uso; no lo fue en la interfaz ni en la
   infraestructura.** Las reglas de negocio se escribieron primero como prueba. Las vistas Razor, los
   controladores y el mapeo de EF Core se escribieron primero y se cubrieron después con pruebas de
   integración y de navegador. Esta distinción es real y se documenta como tal; ver §6.

---

## Iteración 1 — Fundamentos y proveedores

**Objetivo demostrable:** un proveedor puede registrarse, consultarse y eliminarse, y los datos
persisten en PostgreSQL.

**Historias:** H-01 a H-05 (14 puntos). **Puntos completados:** 14. **Velocidad:** 14.

### Qué se construyó

| Entrega | Commit | Contenido |
| ----- | ----- | ----- |
| 1 | `chore: inicializar la estructura de la solución .NET 9` | Cinco proyectos, gestión centralizada de paquetes, `.editorconfig`, analizadores. |
| 2 | `docs(xp): definir visión, historias de usuario y plan de liberación` | Visión y alcance, 22 historias, plan XP. |
| 3 | `feat(dominio): modelar entidades, reglas y servicios de dominio` | Entidades, invariantes, normalización, transiciones, evaluador, selector de nivel. |
| 4 | `feat(aplicacion): definir contratos, resultados y DTO` | Puertos, `Resultado<T>`, DTO, parámetros de consulta. |
| 5 | `feat(persistencia): mapear el modelo a PostgreSQL` | Configuraciones de EF Core, migración inicial, semillas. |
| — | `test(dominio): cubrir con pruebas unitarias las reglas de negocio` | Batería de pruebas del dominio. |
| — | `feat(proveedores): implementar el CRUD de proveedores` | Primer módulo completo de aplicación. |

### TDD en esta iteración

El ciclo se aplicó regla por regla. Un ejemplo textual, la unicidad normalizada:

1. **Rojo.** `ProveedorTests.Crear_ConNombreQueSoloDifiereEnEspacios_ProduceElMismoNombreNormalizado`
   falló: `NormalizadorTexto` todavía no existía.
2. **Verde.** Se implementó `NormalizarNombre` con lo mínimo: recortar, colapsar espacios y pasar a
   mayúsculas invariantes.
3. **Refactorización.** El colapso de espacios pasó a `[GeneratedRegex]`, que compila la expresión en
   tiempo de compilación, y se añadió normalización Unicode al descubrir que dos cadenas visualmente
   idénticas podían diferir en su composición.

### Decisiones tomadas

- **Reloj inyectable desde el primer día.** Al escribir la primera prueba de fecha de cierre quedó
  claro que llamar a `DateTimeOffset.UtcNow` haría la prueba dependiente del momento de ejecución.
  `IReloj` nació de esa prueba, no de un diseño previo.
- **Guardar el texto normalizado en su propia columna.** Normalizar dentro de la consulta habría
  impedido usar el índice único, que es justamente el mecanismo que garantiza la unicidad ante dos
  peticiones simultáneas.

### Problemas encontrados

| Problema | Resolución |
| ----- | ----- |
| No había SDK de .NET ni Docker en el equipo. | Se instaló el SDK de .NET 9 en el perfil de usuario, sin privilegios de administrador. Docker quedó pendiente; su ausencia condicionó la verificación de la iteración 4 (ver §5). |
| Los paquetes más recientes en NuGet eran de la serie 10.x, incompatible con el .NET 9 que exige el enunciado. | Se fijaron todas las versiones a la serie 9.x en `Directory.Packages.props`. `Asp.Versioning` no tiene versión 9.x, así que se usó la 8.1.1, compatible con `net8.0` y por tanto con `net9.0`. |
| El analizador CA1000 marcaba las fábricas estáticas de `Resultado<T>`. | Se suprimió con justificación escrita en `.editorconfig`: es el patrón idiomático para construir resultados tipados. |

---

## Iteración 2 — Ciclo de vida de la licitación

**Objetivo demostrable:** una licitación se crea, se publica, recibe ofertas y muestra la mejor
oferta.

**Historias:** H-06 a H-11 (17 puntos). **Puntos completados:** 17. **Velocidad:** 17.

### Qué se construyó

| Commit | Contenido |
| ----- | ----- |
| `feat(licitaciones): implementar el CRUD y las transiciones de estado` | Máquina de estados con tabla de transiciones y cierre funcional por fecha. |
| `feat(ofertas): implementar el registro de ofertas con sus validaciones` | Monto positivo, tope del presupuesto, una oferta por proveedor. |
| `feat(aprobacion): implementar los niveles de aprobación parametrizables` | Rangos sin traslape, un solo rango abierto, selección por tabla. |
| `feat(moneda): administrar tipos de cambio y conversion referencial CRC/USD` | Un solo tipo activo, conversión con fecha. |
| `test(aplicacion): cubrir los casos de uso sobre repositorios en memoria` | Batería completa de casos de uso. |

### TDD en esta iteración

La máquina de estados es el mejor ejemplo. La prueba se escribió como una tabla exhaustiva: las
nueve combinaciones posibles de estado origen y destino, con tres permitidas y seis prohibidas.
Escribirla primero forzó una decisión de diseño: en lugar de una cadena de condicionales, la
implementación es un conjunto de pares permitidos. Añadir una transición mañana es añadir una fila,
y la prueba exhaustiva detectaría cualquier permiso accidental.

### Refactorizaciones

- **`Licitacion.ActualizarDatos` recibe el monto de la mayor oferta como parámetro.** La primera
  versión consultaba el repositorio desde la entidad, lo que habría metido una dependencia de
  infraestructura en el dominio. El caso de uso lo consulta y se lo pasa.
- **La comparación de rangos de aprobación se extrajo a `SelectorNivelAprobacion`.** Estaba dentro
  del servicio de aplicación; al necesitarla también la evaluación de la mejor oferta, se movió al
  dominio, donde ambas la usan.

---

## Iteración 3 — Interfaz web y API REST

**Objetivo demostrable:** el sistema es usable de extremo a extremo desde el navegador y desde la
API REST.

**Historias:** H-12 a H-17 (24 puntos). **Puntos completados:** 24. **Velocidad:** 24.

### Qué se construyó

| Commit | Contenido |
| ----- | ----- |
| `fix(ofertas): clasificar el ahorro con el porcentaje exacto` | Corrección del defecto descrito abajo. |
| `refactor(dominio): retirar las colecciones de ofertas sin uso` | Simplificación del modelo. |
| `feat(api): exponer la API REST v1 con OpenAPI y ProblemDetails` | Cinco controladores, versionado, errores uniformes. |
| `feat(web): construir la interfaz MVC con tema claro/oscuro y CRC/USD` | Página de inicio, CRUD completo, alternancia de moneda y de tema. |

### Un defecto real encontrado por una prueba

Al escribir las pruebas de clasificación del ahorro apareció un caso límite que la implementación
resolvía mal. Con presupuesto de ₡1 000 000 y oferta de ₡999 999, el ahorro es 0,0001 %. La
implementación redondeaba a dos decimales **antes** de clasificar, obtenía `0,00` y devolvía «Oferta
válida sin ahorro». El enunciado exige «Oferta aceptable» para cualquier ahorro mayor que cero.

La corrección separa los dos usos del mismo número: se clasifica con el porcentaje exacto y se
presenta el redondeado. Quedó registrada en `EvaluadorOfertas.Evaluar` con un comentario que explica
por qué las dos variables existen, para que nadie las «simplifique» de nuevo en una sola.

Es el ejemplo más claro del valor de escribir la prueba pensando en el límite y no en el caso
cómodo: con un ahorro del 5 % la implementación anterior habría pasado sin problema.

### Refactorización: colecciones de navegación retiradas

`Licitacion` y `Proveedor` exponían una colección de ofertas que ningún caso de uso recorría. Todas
las consultas de ofertas pasan por repositorio, con paginación. Mantener la navegación invitaba a
cargar agregados completos en memoria y complicaba el mapeo. Se eliminó y las relaciones quedaron
declaradas solo desde `OfertaConfiguracion`, que es el lado que posee las claves foráneas. La batería
de pruebas estaba verde antes y después.

### Problemas encontrados

| Problema | Resolución |
| ----- | ----- |
| La migración generaba `xmin` como columna real y PostgreSQL la rechazaba, porque es una columna de sistema. | Se sustituyó por una columna `version` explícita, entera, que `RegistrarActualizacion` incrementa. Además resultó mejor: una columna propia puede viajar en un DTO o en un campo oculto del formulario, que es lo que permite detectar la edición concurrente real. |
| La clave foránea del estado de licitación no compilaba: el enum del modelo y el `int` del catálogo no eran compatibles. | El catálogo pasó a tipar su clave como `EstadoLicitacion` con conversión a `int`. |
| Cinco vistas `Index.cshtml` no compilaban por comillas anidadas dentro de un atributo Razor. | La construcción del modelo del componente de paginación se movió al bloque de código de la cabecera de cada vista. |
| Los analizadores marcaban `ToUpper()` dentro de consultas LINQ traducidas a SQL. | Se sustituyó por `EF.Functions.ILike` con un ayudante que escapa `\`, `%` y `_`. De paso se corrigió un problema real: una función sobre la columna habría impedido usar el índice. |

---

## Iteración 4 — Verificación, empaquetado y despliegue

**Objetivo demostrable:** la solución está verificada, empaquetada, desplegable y documentada.

**Historias:** H-18 a H-22 (24 puntos). **Puntos completados:** 24. **Velocidad:** 24.

### Qué se construyó

| Commit | Contenido |
| ----- | ----- |
| `test(integracion): verificar el esquema y la API contra PostgreSQL real` | 73 pruebas con Testcontainers. |
| `test(e2e): recorrer la interfaz con Playwright y configurar la cobertura` | 7 recorridos de navegador y `coverage.runsettings`. |
| `build(docker): empaquetar la aplicación y levantarla junto a PostgreSQL` | Imagen en varias etapas y Docker Compose. |
| `build(k8s): desplegar el sistema en Kubernetes con sondas y límites` | Ocho manifiestos con sondas diferenciadas. |
| `style(codigo): unificar los finales de línea y la codificación` | Corrección detectada al preparar la verificación de formato. |
| `ci(github): bloquear la integración ante cualquier fallo` | Flujo de cinco etapas más una de bloqueo. |
| `docs: completar la documentación del proyecto` | El resto de la documentación. |
| `fix(ci): eliminar la dependencia vulnerable y hacer diagnosticables los fallos` | Testcontainers 4.14.0 y anotaciones desde los resultados de prueba. |
| `fix(web): reparar dos defectos que las pruebas de navegador destaparon` | Validación Unicode en el cliente y errores de servidor visibles. |
| `test(e2e)` ×2 | Diálogo de confirmación atendido y selectores sin ambigüedad. |

### Dónde se verificó cada cosa

**Este equipo no tiene Docker instalado**, así que dos de las tres suites nunca llegaron a ejecutarse
aquí. Se ejecutaron por primera vez en la integración continua, al publicar el repositorio.

| Suite | Compila | Se ejecutó localmente | Se ejecuta en integración continua |
| ----- | ----- | ----- | ----- |
| Unitarias (172) | Sí | Sí, 172 en verde | Sí |
| Integración (73) | Sí | **No** | Sí, en verde |
| Funcionales (7) | Sí | **No** | Sí, en verde |

Lo mismo aplica a `docker compose up --build`: los archivos se escribieron y validaron
sintácticamente aquí, pero el sistema se levantó por primera vez en la integración continua, que
además comprueba que los datos sobreviven a un reinicio de los contenedores. Los manifiestos de
Kubernetes se validan con `kubeconform` en modo estricto contra el esquema 1.30; **no se aplicaron
en un clúster real**, ni aquí ni en la integración continua.

### Lo que encontró la primera ejecución real

Cuatro ejecuciones hicieron falta para llegar al verde, y cada una destapó algo que las 172 pruebas
unitarias no podían ver. Vale la pena el detalle, porque es la mejor justificación de por qué el
enunciado exige los tres niveles de prueba.

| Ejecución | Qué falló | Causa |
| ----- | ----- | ----- |
| 1.ª | Revisión de dependencias y 5 pruebas | Testcontainers arrastraba SSH.NET con una vulnerabilidad alta; y un defecto de la aplicación bloqueaba todos los recorridos de navegador. |
| 2.ª | 1 prueba de integración y 4 funcionales | Se veía por fin el detalle: dos defectos reales de la interfaz y una prueba que afirmaba un comportamiento inexistente. |
| 3.ª | 1 prueba funcional | La licitación no se publicaba: el diálogo de confirmación quedaba sin atender. |
| 4.ª | 1 prueba funcional | Un selector ambiguo resolvía a dos elementos. |
| 5.ª | — | Las seis etapas en verde. |

**El defecto más grave: nadie podía registrar un proveedor desde el navegador.** El patrón que exige
el enunciado, `^[\p{L}\p{N} .,\(\)]+$`, usa clases Unicode. .NET las entiende de forma nativa, pero
una expresión regular de JavaScript solo reconoce `\p{...}` cuando se compila con la marca `u`, y
jquery-validation no la aplica. Sin ella, `\p` se lee como una «p» literal y la clase pasa a admitir
apenas `p`, `L`, `N`, llaves, espacio, punto, coma y paréntesis: el formulario rechazaba
«Constructora Alfa» y cualquier otro nombre real, mientras el servidor lo aceptaba sin problema.

Ni las pruebas unitarias ni las de integración podían detectarlo, porque **ninguna pasa por
JavaScript**. Solo un navegador de verdad lo ve. Se corrigió reemplazando el método de validación por
uno que compila con la marca `u`; el patrón del servidor no cambió.

**El segundo: los errores del servidor eran invisibles en el formulario de licitaciones.** El dominio
nombra el campo `FechaCierre` y el formulario enlaza `FechaCierreLocal`, porque el navegador envía la
fecha sin desplazamiento horario. El mensaje se agregaba al estado del modelo bajo una clave que
ninguna vista muestra y desaparecía: la persona veía el formulario recargado, sin explicación. Ahora,
si la clave no está enlazada, el mensaje cae al resumen de validación.

**Y una prueba equivocada, no un defecto.** Afirmaba `409` al eliminar una licitación con ofertas,
cuando el borrado es lógico y devuelve `204` precisamente para conservarlas como evidencia, que es lo
que pide la sección 8.9. Se reescribió para verificar la regla real y se retiraron dos constantes de
error que ninguna ruta usaba.

Una lección de proceso además de las tres técnicas: los registros de una ejecución solo se descargan
con autenticación, así que el primer fallo llegó sin ningún detalle. Se añadió un paso que convierte
los resultados `.trx` en anotaciones, que sí son públicas. A partir de ahí cada ejecución dijo
exactamente qué prueba fallaba y por qué.

Por eso la etapa `imagen-de-contenedor` del flujo de integración no se limita a construir la imagen:
levanta el sistema completo con Docker Compose, espera a la sonda de preparación, comprueba que la
interfaz, la API y el documento OpenAPI responden, crea un proveedor, reinicia los contenedores y
verifica que el proveedor sigue ahí. Esa comprobación de persistencia es la evidencia de que el
volumen funciona, y se ejecuta en cada cambio enviado al repositorio.

### Un hallazgo al final

Al preparar la verificación de formato, `dotnet format --verify-no-changes` falló en toda la
solución. La causa: `.editorconfig` exigía CRLF mientras el repositorio guarda LF, de modo que la
comprobación daba un resultado en Windows y otro en la integración continua sobre Linux.

Se unificó en LF, también en la copia de trabajo. En el mismo paso se corrigió una regla de
nomenclatura mal planteada: exigía guion bajo a **todos** los campos privados, incluidos constantes y
estáticos de solo lectura, que son valores compartidos y no estado del objeto. Ahora esos llevan
PascalCase.

Es exactamente la clase de defecto que la integración continua existe para encontrar: nadie lo habría
notado trabajando solo en Windows.

---

## Resumen de velocidad

| Iteración | Puntos planificados | Puntos completados | Velocidad |
| ----- | ----- | ----- | ----- |
| 1 | 14 | 14 | 14 |
| 2 | 17 | 17 | 17 |
| 3 | 24 | 24 | 24 |
| 4 | 24 | 24 | 24 |
| **Total** | **79** | **79** | — |

La velocidad crece entre iteraciones porque la primera incluye el costo fijo de arrancar el proyecto
—estructura, estándares, decisiones de diseño— que no se repite. A partir de la tercera, con la
arquitectura estable, el trabajo por punto es más predecible.

## Retroalimentación por iteración

**De la 1 a la 2:** el reloj inyectable y el patrón `Resultado<T>` decidieron la forma de todo lo
demás. Haberlos establecido antes del primer caso de uso evitó reescribir los cuatro módulos
siguientes.

**De la 2 a la 3:** las reglas expresadas como tabla —transiciones, niveles de aprobación— salieron
mucho mejor que las expresadas como condicionales. Se probaron de forma exhaustiva y se cambian sin
tocar código. Donde el enunciado dice «nunca una cadena de if/else», la razón es esta.

**De la 3 a la 4:** escribir las pruebas de integración obligó a releer el esquema real y confirmó
que las decisiones de mapeo eran correctas: índices parciales con su filtro, `numeric(18,2)` para
dinero, claves foráneas con `RESTRICT`. También dejó claro que probar contra PostgreSQL real no es
un capricho del enunciado: los índices únicos parciales y las restricciones CHECK simplemente no
existen en un sustituto en memoria.

**Al cierre:** la lección más cara fue dejar 80 pruebas sin ejecutar hasta el final. Compilaban, se
descubrían y parecían correctas, pero «compila» no es «funciona»: al correrlas de verdad aparecieron
dos defectos reales de la interfaz, uno de ellos capaz de impedir el uso más básico del sistema. Si
hubiera una iteración 5, lo primero sería instalar Docker y ejecutar la batería completa en cada
entrega, no al final.

La segunda lección es sobre el propio ciclo de retroalimentación. Un fallo de integración continua
que no dice qué falló obliga a adivinar, y adivinar produce correcciones a ciegas. Media hora
invertida en convertir los resultados de prueba en anotaciones legibles ahorró varias rondas de
prueba y error, y esa mejora queda en el flujo para cualquier fallo futuro.

---

[← Volver al índice de documentación](README.md)
