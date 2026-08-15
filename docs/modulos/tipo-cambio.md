# Módulo: Tipo de cambio

Historias cubiertas: H-15, H-16. Código principal:
`src/Licitaciones.Domain/Entidades/TipoCambio.cs`,
`src/Licitaciones.Domain/Servicios/ConversorMoneda.cs`,
`src/Licitaciones.Application/Servicios/ServicioTiposCambio.cs`,
`src/Licitaciones.Web/Filtros/FiltroTipoCambioActivo.cs`.

## 1. El principio

**El colón costarricense es la moneda oficial y la fuente de verdad.** Todos los montos se guardan,
comparan y validan en colones. El dólar es una **representación calculada** que no se almacena
nunca:

```
Monto USD = Monto CRC ÷ tipo de cambio
```

Ninguna regla de negocio depende del tipo de cambio. Si no hay ninguno configurado, el sistema sigue
funcionando por completo y los montos se muestran solo en colones.

## 2. Sin acceso a Internet

El enunciado exige que el sistema funcione sin conexión externa. Aquí no hay ninguna llamada a un
servicio de tipos de cambio: el valor se administra desde la propia interfaz y la migración inicial
siembra 520,0000 CRC por USD como tipo activo, de modo que el sistema es utilizable desde el primer
arranque.

## 3. Un solo tipo activo

Solo un registro puede tener `activo = true`. Lo garantiza un índice único parcial:

```sql
CREATE UNIQUE INDEX ux_tipos_cambio_activo ON tipos_cambio (activo) WHERE activo;
```

Un índice sobre una columna booleana parecería inútil —solo hay dos valores—, pero con el filtro
`WHERE activo` solo entran las filas activas, y la unicidad sobre ellas significa exactamente «como
mucho una».

### El problema del instante intermedio

Activar un tipo de cambio implica desactivar el anterior. Si se hicieran las dos cosas y se
confirmaran juntas, EF Core podría ordenar los `UPDATE` de forma que existiera un instante con dos
filas activas, y el índice rechazaría la operación completa.

`DesactivarOtrosAsync` resuelve el orden explícitamente: dentro de la transacción, desactiva el
anterior, **confirma ese cambio intermedio** y luego activa el nuevo. La transacción envolvente
garantiza que ambos pasos ocurren o ninguno.

Está documentado con un comentario en el código y verificado por
`PersistenciaTiposCambioTests.CrearOtroActivo_DesactivaElAnteriorSinViolarElIndiceUnico`, que solo
tiene sentido contra PostgreSQL real.

## 4. Precisión

`numeric(18,4)`: cuatro decimales, no dos.

El tipo de cambio no es un monto, es un **factor de conversión**. Redondearlo a dos decimales
introduciría un error que se multiplica al dividir montos grandes. Con ₡10 000 000 y un tipo de
520,4567, redondear a 520,46 desplaza el resultado en varios dólares.

Los montos convertidos sí se redondean a dos decimales, con `MidpointRounding.AwayFromZero`, que es
la convención esperada al presentar dinero.

## 5. La fecha, siempre visible

El enunciado exige mostrar la fecha del tipo de cambio junto a cualquier importe en dólares. Un monto
en dólares sin esa referencia no significa nada, porque el valor cambia con el tiempo.

La barra superior de la interfaz lo muestra en todas las páginas:

> Tipo de cambio aplicado: **520,0000** CRC por USD · vigente desde el **11/08/2026**

Y la API lo devuelve en cada conversión:

```json
{
  "montoCrc": 1040000.00,
  "montoUsd": 2000.00,
  "crcPorUsd": 520.0000,
  "fechaTipoCambio": "2026-08-11T06:00:00+00:00"
}
```

Cuando no hay tipo activo, la barra cambia a un aviso con un enlace para registrar uno.

## 6. Cómo llega a las vistas

`FiltroTipoCambioActivo` es un filtro de acción registrado globalmente. Consulta el tipo activo
**una sola vez por petición** y lo deja en `ViewData`. Sin él, cada ayudante de monto haría su propia
consulta y una tabla de veinte filas dispararía veinte lecturas idénticas.

El ayudante `Html.Monto` emite las dos representaciones:

```html
<span class="monto">
  <span class="monto-crc">₡8 000 000,00</span>
  <span class="monto-usd d-none">US$ 15 384,62</span>
</span>
```

El JavaScript solo alterna la clase `d-none`. **No calcula nada.** Si el navegador recalculara la
conversión, su redondeo podría diferir del de la API y el mismo monto se vería distinto según dónde
se mirara. Además, la alternancia funciona con JavaScript deshabilitado en el sentido que importa:
los colones, que son el valor oficial, siempre están visibles.

## 7. Reglas

| Regla | Código de error |
| ----- | ----- |
| El valor debe ser mayor que cero. | `TIPO_CAMBIO_INVALIDO` |
| No se puede eliminar el tipo de cambio activo. | `TIPO_CAMBIO_ACTIVO_NO_ELIMINABLE` |
| Convertir sin tipo activo configurado no es posible. | `TIPO_CAMBIO_ACTIVO_REQUERIDO` |

La segunda evita dejar el sistema sin ninguna referencia: para eliminar el activo, primero hay que
activar otro. La interfaz ni siquiera muestra el botón de eliminar en la fila activa.

## 8. Historial

Los tipos de cambio anteriores se conservan, ordenados por fecha de vigencia. No se borran al
desactivarlos: sirven para explicar por qué un informe emitido hace un mes mostraba otro equivalente
en dólares.

## 9. Pruebas

| Prueba | Regla |
| ----- | ----- |
| `ConversorMonedaTests` | La división, el redondeo a dos decimales y el rechazo de un tipo de cambio no positivo. |
| `ServicioTiposCambioTests` | Que crear o activar deja exactamente uno activo; que el activo no se puede eliminar. |
| `PersistenciaTiposCambioTests` | Que el índice único parcial existe y rechaza una segunda fila activa insertada por SQL directo; que la activación funciona contra PostgreSQL real. |
| `ArranqueAplicacionTests` | Que `/api/v1/tipos-cambio/conversion` devuelve el monto en dólares y el tipo aplicado sobre la semilla inicial. |
| `NavegacionE2ETests.AlternarMoneda_*` | Que el botón cambia la representación mostrada en el navegador. |

---

[← Volver al índice de documentación](../README.md)
