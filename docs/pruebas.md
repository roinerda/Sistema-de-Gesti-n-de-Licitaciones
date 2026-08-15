# Estrategia de pruebas

## 1. Pirámide

| Nivel | Proyecto | Qué verifica | Dependencias | Cantidad |
| ----- | ----- | ----- | ----- | ----- |
| Unitarias | `tests/Licitaciones.UnitTests` | Reglas de dominio y casos de uso. | Ninguna: reloj controlado y repositorios en memoria. | 172 |
| Integración | `tests/Licitaciones.IntegrationTests` | Esquema, restricciones, concurrencia y contratos HTTP. | PostgreSQL 16 real en contenedor. | 73 |
| Funcionales | `tests/Licitaciones.FunctionalTests` | Recorridos completos en un navegador. | PostgreSQL real + Chromium. | 7 |

La forma es deliberada. Las pruebas unitarias son rápidas y se ejecutan en cada cambio; las de
integración son más lentas y cubren lo que solo el motor real puede demostrar; las de navegador son
las más lentas y se limitan a los recorridos que de verdad importan.

## 2. Cómo ejecutarlas

```bash
# Solo unitarias: milisegundos, sin dependencias externas
dotnet test tests/Licitaciones.UnitTests/Licitaciones.UnitTests.csproj

# Integración: requiere Docker en marcha (Testcontainers levanta PostgreSQL)
dotnet test tests/Licitaciones.IntegrationTests/Licitaciones.IntegrationTests.csproj

# Funcionales: requiere Docker y los navegadores de Playwright
dotnet build tests/Licitaciones.FunctionalTests/Licitaciones.FunctionalTests.csproj
pwsh tests/Licitaciones.FunctionalTests/bin/Debug/net9.0/playwright.ps1 install chromium
dotnet test tests/Licitaciones.FunctionalTests/Licitaciones.FunctionalTests.csproj

# Todo, con cobertura
dotnet test Licitaciones.sln --collect:"XPlat Code Coverage" --settings coverage.runsettings
```

## 3. Pruebas unitarias

Sin base de datos, sin red y sin reloj real. `RelojFalso` decide qué instante es «ahora» y
`AlmacenEnMemoria` implementa los cinco repositorios y la unidad de trabajo sobre listas.

Esto no es una comodidad: es lo que hace posible probar reglas dependientes del tiempo. La regla del
cierre funcional —una licitación cuya fecha de cierre ya pasó no admite ofertas, aunque su estado
siga diciendo «Publicada»— se verifica adelantando el reloj de la prueba, no esperando.

### Cobertura de las reglas del enunciado

| Regla | Clase de prueba |
| ----- | ----- |
| Transiciones permitidas y prohibidas | `TransicionesLicitacionTests`, `LicitacionTests` |
| Cierre funcional por fecha vencida | `LicitacionTests` |
| Unicidad normalizada de nombre y código | `NormalizadorTextoTests`, `ProveedorTests`, `ServicioProveedoresTests` |
| Conjunto de caracteres del nombre de proveedor | `ProveedorTests` |
| Montos estrictamente mayores que cero | `LicitacionTests`, `OfertaTests` |
| Oferta menor o igual al presupuesto (igual es válida) | `OfertaTests` |
| Presupuesto que no puede bajar de una oferta existente | `ServicioLicitacionesTests` |
| Una oferta por proveedor y licitación | `ServicioOfertasTests` |
| Mejor oferta: menor monto, empate por fecha de registro | `EvaluadorOfertasTests` |
| Clasificación del ahorro con sus cuatro textos exactos | `EvaluadorOfertasTests` |
| Aprobador desde la tabla, rangos sin traslape, un solo rango abierto | `NivelAprobacionTests`, `ServicioNivelesAprobacionTests` |
| Un solo tipo de cambio activo | `ServicioTiposCambioTests` |
| Conversión CRC a USD y su redondeo | `ConversorMonedaTests` |
| Borrado lógico que conserva las ofertas | `ServicioProveedoresTests` |

### Un caso que encontró un error real

`EvaluadorOfertasTests.Evaluar_ConAhorroMinusculo_SiguePresentandoloComoOfertaAceptable` fue la
prueba que destapó un defecto de clasificación. Con un presupuesto de ₡1 000 000 y una oferta de
₡999 999, el ahorro es 0,0001 %, que redondeado a dos decimales da `0,00`. La implementación
clasificaba sobre el valor **redondeado** y devolvía «Oferta válida sin ahorro», cuando el enunciado
exige «Oferta aceptable» para cualquier ahorro mayor que cero.

La corrección separó los dos usos: se clasifica con el porcentaje exacto y se presenta el
redondeado. Está en el commit `fix(ofertas): clasificar el ahorro con el porcentaje exacto`.

## 4. Pruebas de integración

Contra **PostgreSQL 16 real**, levantado con Testcontainers usando la misma imagen que se despliega
(`postgres:16-alpine`). El enunciado prohíbe sustituirlo por SQLite, y estas pruebas explican por
qué: verifican comportamientos que SQLite no tiene.

Un contenedor se comparte por toda la serie y **cada prueba** obtiene su propia base de datos recién
migrada, que se elimina al terminar (xUnit crea una instancia de la clase por cada método de
prueba). Así ninguna prueba depende del orden ni arrastra datos de otra, y todas ven exactamente lo
que vería un despliegue nuevo, semillas incluidas. El precio es crear y migrar una base por prueba;
a esta escala compensa frente a la fragilidad de compartir estado.

| Clase | Qué demuestra |
| ----- | ----- |
| `EsquemaBaseDatosTests` | Que las columnas de dinero son `numeric(18,2)`; que los índices únicos parciales existen con su filtro; que las siete restricciones CHECK y las claves foráneas están declaradas; que las semillas cargan estados, niveles y tipo de cambio inicial. |
| `PersistenciaProveedoresTests` | Que el índice único rechaza nombres que solo difieren en mayúsculas o espacios, incluso saltándose el caso de uso; que el borrado lógico libera el nombre; que el CHECK rechaza un nombre en blanco escrito por SQL directo. |
| `PersistenciaOfertasTests` | Que un proveedor no puede ofertar dos veces en la misma licitación; que el mismo proveedor sí puede ofertar en licitaciones distintas; que el error de integridad referencial llega traducido, sin código SQL ni consulta en el mensaje; que el borrado lógico del proveedor conserva sus ofertas. |
| `PersistenciaTiposCambioTests` | Que activar un tipo de cambio desactiva el anterior dentro de la misma transacción, sin violar el índice único parcial; que insertar una segunda fila activa por SQL directo es rechazado por el motor. |
| `ConcurrenciaOptimistaTests` | Que dos contextos que editan el mismo registro producen un conflicto; que una versión ya consumida se rechaza aunque la edición llegue después; que cada actualización exitosa incrementa la versión. |
| `ProveedoresApiTests`, `LicitacionesApiTests`, `OfertasApiTests` | Que la aplicación completa devuelve 200/201/204/400/404/409/422 donde corresponde, con `codigoError` e `identificadorCorrelacion`, y que **ninguna respuesta de error contiene trazas, nombres de ensamblado, rutas del sistema ni referencias a Npgsql**. |
| `ArranqueAplicacionTests` | Que la interfaz responde HTML, que las dos sondas de salud responden, que el documento OpenAPI describe los cinco recursos y que la conversión CRC/USD funciona sobre la semilla. |

Las pruebas de API ejercitan el mismo `Program` que se publica en el contenedor: enrutado,
versionado, filtros, validación de modelo y traducción de errores. Solo se sustituyen el contexto de
datos, para aislar cada clase, y el reloj, para que las reglas de tiempo sean deterministas.

## 5. Pruebas funcionales

Playwright con Chromium contra la aplicación servida por **Kestrel en un puerto real**, no por un
servidor en memoria: un navegador necesita una dirección TCP a la que conectarse.

| Prueba | Recorrido |
| ----- | ----- |
| `PaginaDeInicio_PresentaLosModulosYElTipoDeCambioVigente` | La página de inicio enlaza los módulos y muestra el tipo de cambio aplicado. |
| `FlujoCompleto_LlevaDeLaLicitacionEnBorradorHastaLaMejorOferta` | Registrar dos proveedores, crear la licitación, publicarla, registrar dos ofertas y comprobar que el detalle muestra la mejor oferta, la clasificación y el aprobador de la tabla. |
| `Proveedor_ConNombreDuplicado_MuestraUnMensajeDeError` | El mismo nombre con otras mayúsculas y espacios se rechaza con un mensaje visible. |
| `Licitacion_ConFechaDeCierrePasada_NoSeCrea` | La fecha vencida se rechaza en el formulario. |
| `AlternarMoneda_MuestraElEquivalenteEnDolaresSinCambiarElValorOficial` | El botón de moneda cambia la representación mostrada. |
| `AlternarTema_CambiaElTemaYLoConservaAlNavegar` | El tema oscuro se aplica y sobrevive a la navegación, antes del primer pintado. |
| `Eliminar_UnProveedor_PideConfirmacionAntesDeBorrar` | Al cancelar la confirmación, el registro sigue ahí. |

## 6. Cobertura

Umbrales del enunciado: **80 % o más** en dominio y aplicación, **70 % o más** en total.

Medición con `coverlet.collector` y la configuración de [`coverage.runsettings`](../coverage.runsettings),
que limita la medición al código propio y excluye las migraciones generadas por Entity Framework
Core, donde no hay decisiones que probar.

Solo con las pruebas unitarias:

| Ensamblado | Líneas | Ramas |
| ----- | ----- | ----- |
| `Licitaciones.Domain` | 88,19 % | 86,61 % |
| `Licitaciones.Application` | 83,94 % | 61,11 % |

La cobertura de `Licitaciones.Infrastructure`, `Licitaciones.Api` y `Licitaciones.Web` proviene de
las pruebas de integración y funcionales, así que la cifra total solo es significativa cuando se
ejecuta la suite completa con Docker disponible.

La integración continua combina los tres informes con ReportGenerator y verifica los umbrales con
[`.github/scripts/verificar_cobertura.py`](../.github/scripts/verificar_cobertura.py). Si alguno no
se cumple, la ejecución falla y el cambio no entra.

## 7. Qué requiere cada nivel

| | Unitarias | Integración | Funcionales |
| ----- | ----- | ----- | ----- |
| SDK de .NET 9 | Sí | Sí | Sí |
| Docker en marcha | No | Sí | Sí |
| Navegador de Playwright | No | No | Sí |

En un equipo sin Docker, las pruebas unitarias siguen ejecutándose por completo; las otras dos suites
compilan pero no pueden correr. La integración continua sí tiene Docker, de modo que la suite
completa se ejecuta en cada cambio enviado al repositorio.

## 8. Convenciones

- Nombres en español con la forma `Sujeto_Situacion_ResultadoEsperado`, de modo que el informe de
  fallos se lee como una lista de reglas incumplidas.
- Una aserción por concepto. Si una prueba necesita muchas aserciones sin relación, son varias
  pruebas.
- Nada de esperas fijas. El tiempo se controla con `RelojFalso` y la interfaz con las esperas
  automáticas de Playwright.
- Cada prueba construye lo que necesita; ninguna depende del estado que dejó otra.
