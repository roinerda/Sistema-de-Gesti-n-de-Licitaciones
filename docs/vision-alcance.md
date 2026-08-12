# Visión y alcance

## 1. Propósito

El **Sistema de Gestión de Licitaciones** permite administrar procesos de compra desde su redacción hasta
la determinación de la mejor oferta y del nivel de aprobación que corresponde al monto adjudicado.

El sistema resuelve tres problemas concretos del cliente:

1. **Trazabilidad del proceso.** Cada licitación tiene un ciclo de vida explícito (Borrador → Publicada →
   Cerrada) con transiciones controladas, de modo que nadie pueda reabrir un proceso ya cerrado ni recibir
   ofertas fuera de plazo.
2. **Comparación objetiva de ofertas.** El sistema calcula la mejor oferta, el porcentaje de ahorro y la
   clasificación del resultado con reglas idénticas para todos los procesos.
3. **Aprobación según monto.** El aprobador se obtiene de una tabla parametrizable, no de reglas escritas en
   el código, de modo que la organización pueda cambiar sus umbrales sin recompilar el sistema.

## 2. Moneda oficial

El **colón costarricense (CRC)** es la moneda oficial y la única fuente de verdad. Todos los montos se
almacenan en CRC con precisión `numeric(18,2)`.

La visualización en **dólares estadounidenses (USD)** es una representación calculada:

```
Monto USD = Monto CRC / Tipo de cambio (CRC por USD)
```

La conversión nunca modifica los valores persistidos, siempre muestra la fecha del tipo de cambio utilizado
y funciona sin acceso a Internet porque el tipo de cambio se administra localmente.

## 3. Alcance incluido

| Área | Contenido |
| ----- | ----- |
| Licitaciones | CRUD completo, cambio de estado, borrado lógico, consulta de ofertas, mejor oferta, clasificación y aprobador. |
| Proveedores | CRUD completo con unicidad normalizada del nombre, borrado lógico y consulta de ofertas relacionadas. |
| Ofertas | CRUD completo con validación de estado, vencimiento, duplicidad y límite presupuestario. |
| Niveles de aprobación | CRUD completo de rangos no traslapados, con un único rango abierto. |
| Tipos de cambio | CRUD completo y selección del registro activo. |
| Interfaz web | Landing page explicativa, navegación, modo claro/oscuro, alternancia CRC/USD, formularios validados y tablas con paginación, filtrado y ordenamiento. |
| API REST | Endpoints versionados con DTO, OpenAPI, códigos HTTP correctos y `ProblemDetails`. |
| Operación | Docker Compose, manifiestos de Kubernetes e integración continua con GitHub Actions. |

## 4. Alcance excluido

Estas decisiones mantienen el **diseño simple** exigido por XP: no se construye nada que las historias
vigentes no requieran.

| Fuera de alcance | Razón |
| ----- | ----- |
| Autenticación y autorización de usuarios | Ninguna historia del enunciado describe roles ni credenciales. Agregar seguridad especulativa complicaría el modelo sin criterio de aceptación que la verifique. |
| Consumo de un servicio externo de tipo de cambio | El enunciado exige explícitamente que la solución funcione sin Internet. |
| Adjudicación formal, contratos o expedientes | El alcance termina en la determinación de la mejor oferta y su aprobador. |
| Notificaciones por correo | No hay historia que lo pida. |
| Multi-moneda más allá de CRC/USD | El enunciado define exactamente dos monedas. |

## 5. Actores

| Actor | Descripción |
| ----- | ----- |
| **Cliente / Analista de compras** | Redacta licitaciones, las publica, registra ofertas y consulta resultados. Es el rol del *cliente* en la terminología XP: define y prioriza las historias. |
| **Sistema integrado** | Cualquier aplicación que consuma la API REST versionada. |

## 6. Criterios de éxito del proyecto

1. El flujo funcional mínimo del enunciado (sección 5.3) se puede ejecutar de extremo a extremo desde el
   navegador y también desde la API REST.
2. Toda regla de negocio verificable está cubierta por al menos una prueba automatizada.
3. `docker compose up --build` levanta la solución completa sin pasos manuales.
4. Los manifiestos de `/k8s` despliegan aplicación y base de datos con persistencia comprobable.
5. La integración continua queda en verde en la entrega final.

## 7. Glosario

| Término | Definición |
| ----- | ----- |
| **Licitación** | Proceso de compra con código único, presupuesto estimado y fecha de cierre. |
| **Oferta** | Propuesta económica de un proveedor para una licitación. Máximo una por proveedor y licitación. |
| **Mejor oferta** | Oferta válida de menor monto en CRC; en empate gana la registrada primero. |
| **Ahorro** | `((Presupuesto − Mejor oferta) / Presupuesto) × 100`. |
| **Cierre funcional** | Estado en que una licitación ya no admite actividad, sea por estado `Cerrada` o porque se alcanzó la fecha de cierre. |
| **Nivel de aprobación** | Rango de montos en CRC asociado a la instancia que aprueba. |
| **Normalización** | Transformación de un texto para comparar unicidad: recorte, colapso de espacios, Unicode FormC y mayúsculas invariantes. |

---

[← Volver al índice de documentación](README.md)
