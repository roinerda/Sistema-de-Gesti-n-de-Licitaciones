# Módulo: Proveedores

Historias cubiertas: H-03, H-04, H-05. Código principal:
`src/Licitaciones.Domain/Entidades/Proveedor.cs`,
`src/Licitaciones.Domain/Normalizacion/NormalizadorTexto.cs`,
`src/Licitaciones.Application/Servicios/ServicioProveedores.cs`.

## 1. Qué representa

Una empresa o persona que puede presentar ofertas. Su único dato propio es el nombre, y ese nombre
tiene que ser inequívoco.

## 2. Unicidad normalizada

El enunciado es explícito: dos proveedores no pueden diferir solo en mayúsculas, espacios repetidos o
composición Unicode. `NormalizadorTexto.NormalizarNombre` aplica cuatro pasos, en este orden:

1. Recorte de espacios laterales.
2. Colapso de espacios repetidos a uno solo, con una expresión regular compilada en tiempo de
   compilación (`[GeneratedRegex(@"\s+")]`).
3. Normalización Unicode a forma C, que unifica «á» escrita como un carácter y «á» escrita como «a»
   más tilde combinante.
4. Mayúsculas invariantes.

| Entrada | Nombre guardado | Nombre normalizado |
| ----- | ----- | ----- |
| `Constructora Alfa` | `Constructora Alfa` | `CONSTRUCTORA ALFA` |
| `  constructora   alfa  ` | `constructora alfa` | `CONSTRUCTORA ALFA` |
| `CONSTRUCTORA ALFA` | `CONSTRUCTORA ALFA` | `CONSTRUCTORA ALFA` |

Las tres colisionan entre sí. Nótese que el **nombre visible se conserva tal como se escribió**
(salvo la limpieza de espacios): la normalización sirve para comparar, no para reemplazar lo que la
persona escribió.

El paso 3 no es un detalle académico. Sin él, dos cadenas que se ven idénticas en pantalla podrían
convivir en la tabla, y nadie entendería por qué el sistema las considera distintas.

### Dónde se verifica

En tres lugares, a propósito:

| Capa | Mecanismo | Para qué |
| ----- | ----- | ----- |
| Interfaz | Anotaciones y validación no intrusiva de jQuery. | Aviso inmediato, sin viaje al servidor. |
| Servidor | `ServicioProveedores` consulta `ExisteNombreAsync` antes de guardar. | Mensaje claro con el campo asociado. |
| PostgreSQL | Índice único parcial `ux_proveedores_nombre_normalizado`. | Lo único que dos peticiones simultáneas no pueden burlar. |

La tercera es la que de verdad garantiza la regla. Las dos primeras existen para dar buenos mensajes.

## 3. Conjunto de caracteres permitido

```
^[\p{L}\p{N} .,\(\)]+$
```

Letras de cualquier idioma, dígitos, espacio, punto, coma y paréntesis. Es lo que exige el enunciado
y admite razones sociales reales como `Constructora Alfa S.A.` o `Servicios Integrados (Zona Norte)`,
mientras rechaza caracteres de control y marcado.

Se usa `\p{L}` y no `[a-zA-Z]` porque los nombres costarricenses llevan tildes y eñes.

## 4. Otras reglas

| Regla | Código de error |
| ----- | ----- |
| El nombre es obligatorio y no puede ser solo espacios. | `PROVEEDOR_NOMBRE_REQUERIDO` |
| Máximo 150 caracteres, medidos después de limpiar espacios. | `PROVEEDOR_NOMBRE_DEMASIADO_LARGO` |
| Un proveedor dado de baja no admite modificaciones. | `PROVEEDOR_ELIMINADO` |

## 5. Borrado lógico

Se marca `deleted_at`; la fila permanece. La operación es **idempotente**: eliminar dos veces no
falla, porque un reintento tras un error de red no debería producir un error distinto.

Consecuencias:

- Las ofertas del proveedor **se conservan**. Son la evidencia de por qué se adjudicó lo que se
  adjudicó.
- El nombre **queda libre** para un proveedor nuevo, porque el índice único es parcial.
- El proveedor desaparece de los listados salvo que se pida `IncluirEliminados=true`.
- La licitación sigue mostrando su nombre en las ofertas, con la marca de dado de baja.

`Restaurar` revierte la baja, también de forma idempotente.

## 6. Búsqueda

El repositorio usa `EF.Functions.ILike` sobre la columna normalizada, con un ayudante que escapa
`\`, `%` y `_` para que un nombre que contenga esos caracteres no se interprete como comodín.

No se aplica `ToUpper()` dentro de la consulta: una función sobre la columna impediría usar el
índice y obligaría a recorrer la tabla completa.

## 7. Pruebas

| Prueba | Regla |
| ----- | ----- |
| `NormalizadorTextoTests` | Los cuatro pasos de normalización, incluida la composición Unicode. |
| `ProveedorTests` | Nombre obligatorio, longitud máxima, conjunto de caracteres, borrado idempotente. |
| `ServicioProveedoresTests` | Duplicado detectado por el caso de uso, borrado lógico, listado que oculta los eliminados. |
| `PersistenciaProveedoresTests` | El índice único de PostgreSQL rechaza los duplicados aunque se escriba saltándose el caso de uso; el borrado lógico libera el nombre; el CHECK rechaza un nombre en blanco. |
| `ProveedoresApiTests` | `201` con `Location`, `409` en duplicado, `400` en caracteres inválidos, `409` en conflicto de concurrencia. |

---

[← Volver al índice de documentación](../README.md)
