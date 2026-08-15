# Módulo: Ofertas

Historias cubiertas: H-09, H-10, H-11. Código principal:
`src/Licitaciones.Domain/Entidades/Oferta.cs`,
`src/Licitaciones.Domain/Servicios/EvaluadorOfertas.cs`,
`src/Licitaciones.Application/Servicios/ServicioOfertas.cs`.

## 1. Qué representa

La propuesta económica de un proveedor para una licitación: un monto en colones y el instante en que
se registró.

## 2. Reglas de registro

| Regla | Dónde vive | Código de error | HTTP |
| ----- | ----- | ----- | ----- |
| El monto es estrictamente mayor que cero. | `Oferta.Crear` + CHECK en el motor | `OFERTA_MONTO_INVALIDO` | 422 |
| El monto no puede superar el presupuesto. **Igualarlo sí es válido.** | `Oferta.Crear` | `OFERTA_SUPERA_PRESUPUESTO` | 422 |
| La licitación debe estar publicada. | `Licitacion.GarantizarQueAceptaOfertas` | `OFERTA_LICITACION_NO_PUBLICADA` | 422 |
| La fecha de cierre no debe haber pasado. | El mismo método, contra `IReloj.Ahora` | `OFERTA_VENCIDA` | 422 |
| Un proveedor presenta una sola oferta por licitación. | Caso de uso + índice único compuesto | `OFERTA_DUPLICADA` | 409 |
| El proveedor debe existir y estar vigente. | `ServicioOfertas` | `RECURSO_NO_ENCONTRADO` | 404 |

La segunda regla se comprueba con `>` y no con `>=`. El enunciado dice que una oferta igual al
presupuesto es válida, y esa frontera es exactamente el tipo de detalle que una prueba de límite debe
fijar: `OfertaTests` incluye un caso con monto idéntico al presupuesto que debe pasar.

## 3. Mejor oferta

```csharp
ofertas.OrderBy(o => o.MontoOfertadoCrc)
       .ThenBy(o => o.FechaRegistro)
       .ThenBy(o => o.Id)
       .FirstOrDefault();
```

Menor monto; en empate, la registrada primero. El tercer criterio, el identificador, no está en el
enunciado: se añadió para que el resultado sea **estable** si dos ofertas comparten monto e instante
exacto. Sin él, el orden lo decidiría el motor y la misma consulta podría devolver resultados
distintos.

El índice `ix_ofertas_licitacion_monto_fecha` cubre este orden completo.

## 4. Clasificación del ahorro

```
ahorro % = (presupuesto − mejor oferta) ÷ presupuesto × 100
```

| Condición | Texto exacto |
| ----- | ----- |
| No hay ofertas válidas | `Sin ofertas válidas` |
| Ahorro ≥ 10 % | `Oferta conveniente` |
| Ahorro > 0 % y < 10 % | `Oferta aceptable` |
| Ahorro = 0 % | `Oferta válida sin ahorro` |

Los textos son literales del enunciado y están en `ClasificacionOfertaExtensiones.Descripcion()`.
Las pruebas los comparan carácter por carácter, tildes incluidas.

### El caso límite que reveló un defecto

Con presupuesto de ₡1 000 000 y oferta de ₡999 999, el ahorro es 0,0001 %. Redondeado a dos
decimales da `0,00`, y la primera implementación —que clasificaba sobre el valor ya redondeado—
devolvía «Oferta válida sin ahorro».

El enunciado exige «Oferta aceptable» para **cualquier** ahorro mayor que cero. La corrección separa
los dos usos del mismo número:

```csharp
decimal ahorroExacto = CalcularPorcentajeAhorroExacto(presupuestoCrc, mejor.MontoOfertadoCrc);
decimal ahorroPresentado = decimal.Round(ahorroExacto, 2, MidpointRounding.AwayFromZero);

return new EvaluacionOfertas(mejor, ahorroPresentado, Clasificar(ahorroExacto));
```

Se clasifica con el exacto y se presenta el redondeado. El comentario junto al código explica por qué
las dos variables existen, para que nadie las vuelva a fundir en una sola.

`MidpointRounding.AwayFromZero` y no el redondeo bancario por omisión de .NET: en presentación de
montos, la convención esperada es que 0,125 se muestre como 0,13.

## 5. Aprobador

El aprobador de la adjudicación se obtiene de la tabla de niveles de aprobación con el monto de la
mejor oferta. No se guarda en la oferta ni en la licitación: se calcula al consultar, para que un
cambio en la parametrización se refleje en todos los expedientes. Ver
[niveles-aprobacion.md](niveles-aprobacion.md).

## 6. Edición y eliminación

- Editar una oferta solo cambia el monto, y vuelve a comprobar todas las reglas: la licitación debe
  seguir aceptando ofertas y el nuevo monto seguir dentro del presupuesto.
- Las ofertas se eliminan **físicamente**, no de forma lógica. A diferencia de proveedores y
  licitaciones, una oferta retirada no deja rastro que otro registro necesite: retirarla es una
  operación legítima mientras la licitación siga abierta.
- Si la licitación está cerrada funcionalmente, ni editar ni eliminar están disponibles: la interfaz
  ni siquiera muestra los botones.

## 7. Consultas

| Ruta | Devuelve |
| ----- | ----- |
| `GET /api/v1/ofertas?LicitacionId=…` | Ofertas de una licitación, ordenadas por monto. |
| `GET /api/v1/ofertas?ProveedorId=…` | Ofertas de un proveedor. |
| `GET /api/v1/licitaciones/{id}/ofertas` | Lo mismo, desde el recurso licitación. |
| `GET /api/v1/licitaciones/{id}/mejor-oferta` | Mejor oferta, ahorro, clasificación y aprobador. |

Los listados cargan licitación y proveedor con `Include`, porque el DTO muestra el código y el
nombre. Traerlos en la misma consulta evita el problema de N+1: una consulta por fila para resolver
un nombre.

## 8. Pruebas

| Prueba | Regla |
| ----- | ----- |
| `OfertaTests` | Monto positivo, tope del presupuesto, caso de igualdad exacta, licitación que no acepta ofertas. |
| `EvaluadorOfertasTests` | Mejor oferta, desempate por fecha, los cuatro textos de clasificación y el caso del ahorro minúsculo. |
| `ServicioOfertasTests` | Oferta duplicada, proveedor inexistente, edición con revalidación. |
| `PersistenciaOfertasTests` | El índice único compuesto en PostgreSQL, el CHECK del monto, el error de integridad traducido y la conservación de ofertas tras dar de baja al proveedor. |
| `OfertasApiTests` | `201`, `409` en duplicado, `422` sobre presupuesto y sobre licitación en borrador, `400` con monto cero. |

---

[← Volver al índice de documentación](../README.md)
