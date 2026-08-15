# Módulo: Niveles de aprobación

Historia cubierta: H-14. Código principal:
`src/Licitaciones.Domain/Entidades/NivelAprobacion.cs`,
`src/Licitaciones.Domain/Servicios/SelectorNivelAprobacion.cs`,
`src/Licitaciones.Application/Servicios/ServicioNivelesAprobacion.cs`.

## 1. Qué representa

La tabla que dice **quién debe aprobar** una adjudicación según el monto de la mejor oferta. Cada
nivel es un rango de montos en colones y el nombre de la instancia responsable.

Los valores iniciales, sembrados por la migración, son los del enunciado:

| Monto mínimo (CRC) | Monto máximo (CRC) | Aprobador |
| ----- | ----- | ----- |
| 0,01 | 999 999,99 | Encargado de área |
| 1 000 000,00 | 9 999 999,99 | Gerencia |
| 10 000 000,00 | *(sin límite)* | Junta Directiva |

## 2. Tabla, nunca una cadena de condicionales

El enunciado lo exige expresamente y la razón es práctica: los umbrales de aprobación cambian por
decisión administrativa, no por decisión técnica. Con una cadena de `if/else`, subir el techo de
Gerencia obligaría a recompilar, volver a probar y volver a desplegar el sistema.

La selección recorre los niveles almacenados:

```csharp
public static NivelAprobacion? Seleccionar(IEnumerable<NivelAprobacion> niveles, decimal montoCrc) =>
    niveles.FirstOrDefault(n => n.CubreMonto(montoCrc));
```

Añadir, mover o quitar un nivel es editar filas desde la interfaz.

## 3. Reglas de los rangos

| Regla | Motivo | Código de error |
| ----- | ----- | ----- |
| El monto mínimo es mayor que cero. | Un rango que empieza en cero no corresponde a ninguna adjudicación real. | `NIVEL_APROBACION_RANGO_INVALIDO` |
| El monto máximo, si existe, es mayor o igual al mínimo. | Un rango invertido no cubriría nada. | `NIVEL_APROBACION_RANGO_INVALIDO` |
| Los rangos no se traslapan. | Con traslape, el aprobador dependería del orden de lectura. | `NIVEL_APROBACION_RANGO_TRASLAPADO` |
| Solo un rango puede quedar abierto por arriba. | Dos rangos sin techo se traslapan por definición. | `NIVEL_APROBACION_RANGO_ABIERTO_DUPLICADO` |
| El aprobador es obligatorio, máximo 120 caracteres. | Un nivel sin responsable no sirve para nada. | `NIVEL_APROBACION_APROBADOR_REQUERIDO` |

La tercera es la interesante. Si existieran los rangos `[1 000 000 – 9 999 999]` y
`[5 000 000 – 20 000 000]`, un monto de ₡6 000 000 caería en los dos y el aprobador dependería de
cuál se leyera primero. `GarantizarRangoConsistente` compara el rango propuesto contra todos los
existentes (excluyendo el que se está editando) antes de guardar.

### Cómo se detecta el traslape

Dos rangos `[a₁, b₁]` y `[a₂, b₂]` se traslapan si `a₁ ≤ b₂` y `a₂ ≤ b₁`, tratando un máximo nulo
como infinito. Es la comprobación estándar de intervalos, y se hace en el dominio para que valga
igual venga la petición de la web o de la API.

## 4. Huecos

La validación impide traslapes pero **no** exige cobertura continua. Es legítimo configurar
`[0,01 – 999 999,99]` y `[10 000 000 – ∞)` dejando un hueco intermedio.

Un monto que cae en el hueco no tiene aprobador. La API devuelve `404` en
`/niveles-aprobacion/aprobador` y la interfaz muestra «Sin nivel de aprobación configurado».

Se decidió no obligar a la continuidad porque forzarla haría imposible construir la tabla paso a
paso: al crear el primer nivel, cualquier configuración sería incompleta. La ausencia de aprobador es
un estado visible y corregible, no un error del sistema.

## 5. Restricciones en el motor

| Restricción | Expresión |
| ----- | ----- |
| `ck_niveles_aprobacion_minimo_positivo` | `monto_minimo_crc > 0` |
| `ck_niveles_aprobacion_rango_coherente` | `monto_maximo_crc IS NULL OR monto_maximo_crc >= monto_minimo_crc` |
| `ux_niveles_aprobacion_monto_minimo` | Índice único sobre el monto mínimo. |

El índice único sobre el mínimo no cubre todos los traslapes posibles —PostgreSQL podría hacerlo con
una restricción de exclusión sobre rangos—, pero sí el caso más frecuente: dos niveles que arrancan
en el mismo monto. La validación completa vive en el dominio.

## 6. Uso desde otros módulos

`ServicioLicitaciones` pide el aprobador al construir la evaluación de la mejor oferta. El resultado
viaja en `MejorOfertaDto.Aprobador` y se muestra en el detalle de la licitación.

**El aprobador no se guarda.** Si se guardara, cambiar la tabla dejaría los expedientes existentes
mostrando un nivel que ya no rige, y nadie sabría cuál de los dos es el válido.

## 7. Eliminación

Los niveles se eliminan **físicamente**: no hay registros que dependan de ellos, porque el aprobador
se calcula al consultar. Eliminar un nivel deja sin aprobador los montos de su rango hasta que se
configure otro, y la interfaz lo advierte en la pantalla de confirmación.

## 8. Pruebas

| Prueba | Regla |
| ----- | ----- |
| `NivelAprobacionTests` | Selección por rango, monto en el límite inferior y superior, rango abierto, detección de traslape y de segundo rango abierto. |
| `ServicioNivelesAprobacionTests` | Las mismas reglas desde el caso de uso, incluida la exclusión del propio registro al editar. |
| `EsquemaBaseDatosTests` | Que las semillas cargan exactamente los tres niveles del enunciado y que las restricciones CHECK existen. |
| `LicitacionesApiTests` | Que la mejor oferta de ₡8 000 000 devuelve «Gerencia», tomado de la tabla. |

---

[← Volver al índice de documentación](../README.md)
