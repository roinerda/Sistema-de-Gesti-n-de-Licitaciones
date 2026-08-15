# Módulo: Interfaz web

Historias cubiertas: H-12, H-13, H-17. Código principal: `src/Licitaciones.Web/`.

## 1. Tecnología

ASP.NET Core MVC con vistas Razor. Bootstrap 5.3, jQuery y jquery-validation **servidos desde el
propio sitio**, no desde una red de distribución de contenido.

Eso último es un requisito, no una preferencia: el enunciado exige que el sistema funcione sin acceso
a Internet. Con las bibliotecas en una CDN, la aplicación se vería rota en una máquina aislada.

## 2. Navegación

| Ruta | Contenido |
| ----- | ----- |
| `/` | Página de inicio: explica el flujo completo y enlaza cada módulo. |
| `/Licitaciones` | Listado con búsqueda, filtro por estado, orden y paginación. |
| `/Licitaciones/Detalle/{id}` | Datos, mejor oferta, aprobador, transiciones disponibles y ofertas recibidas. |
| `/Proveedores` | Listado y CRUD. |
| `/Ofertas` | Listado filtrable por licitación y por proveedor. |
| `/NivelesAprobacion` | Parametrización de los rangos de aprobación. |
| `/TiposCambio` | Administración del tipo de cambio. |
| `/swagger` | Documentación interactiva de la API. |

Todas las páginas comparten `_Layout.cshtml`, con la barra de navegación, el aviso permanente del
tipo de cambio aplicado y los dos botones de alternancia.

## 3. Página de inicio

No es un cartel de bienvenida. Explica el flujo del sistema en el orden en que hay que recorrerlo
—registrar proveedores, crear la licitación, publicarla, recibir ofertas, consultar la mejor oferta y
su aprobador— y enlaza cada paso. Alguien que abre el sistema por primera vez puede completar el
recorrido sin manual.

## 4. Alternancia de moneda

El botón «Ver en USD» cambia qué representación se muestra. El servidor emite las dos:

```html
<span class="monto">
  <span class="monto-crc">₡8 000 000,00</span>
  <span class="monto-usd d-none">US$ 15 384,62</span>
</span>
```

El JavaScript alterna la clase `d-none` y guarda la preferencia en `localStorage`. **No calcula
nada**: el valor oficial en colones nunca se recalcula en el navegador, de modo que el redondeo es
siempre el mismo que el de la API.

Si no hay tipo de cambio activo, el aviso superior lo indica y los montos se muestran solo en
colones.

## 5. Tema claro y oscuro

Se usa el atributo `data-bs-theme` de Bootstrap 5.3, que ya trae las variables de color de ambos
temas.

El detalle que importa es **cuándo** se aplica. `wwwroot/js/tema-inicial.js` se carga en el `<head>`,
antes de que el navegador pinte nada, y fija el atributo desde `localStorage`. Aplicarlo al final de
la página produciría un destello blanco en cada navegación para quien usa el tema oscuro.

El botón alterna, guarda la preferencia y actualiza `aria-pressed` para que un lector de pantalla
anuncie el estado.

## 6. Validación en cliente y en servidor

Las anotaciones de los DTO alimentan las dos:

| Capa | Mecanismo | Qué aporta |
| ----- | ----- | ----- |
| Cliente | jquery-validation no intrusiva, generada desde las mismas anotaciones. | Aviso inmediato, sin viaje al servidor. |
| Servidor | `ModelState` más las reglas del dominio. | La verdad. El cliente puede desactivar JavaScript. |

Cuando un caso de uso devuelve un error con `Campo`, `AgregarErrorAlModelo` lo coloca en el
`ModelState` de esa propiedad, de modo que el mensaje aparece junto al control correspondiente y no
como un aviso genérico al principio del formulario.

## 7. Fechas y calendario

Los formularios usan `<input type="datetime-local">` y `<input type="date">`, que muestran el
selector nativo del navegador y el formato local sin JavaScript adicional.

El valor se enlaza como `DateTime` sin desplazamiento y se interpreta como **hora de Costa Rica**;
`ZonaHorariaCostaRica.DesdeHoraLocal` lo convierte a `DateTimeOffset` en UTC antes de guardarlo. La
presentación hace el camino inverso.

Enlazarlo directamente como `DateTimeOffset` habría sido el error fácil: el navegador no envía
desplazamiento en esos controles y .NET habría asumido UTC, corriendo cada fecha seis horas.

## 8. Confirmación antes de eliminar

Toda eliminación pasa por dos pasos: una pantalla de confirmación que muestra qué se va a eliminar y
qué consecuencias tiene, y un diálogo `confirm` del navegador enlazado por el atributo
`data-confirmar`.

La pantalla intermedia es la que de verdad protege —funciona sin JavaScript—; el diálogo evita el
clic accidental.

## 9. Mensajes

Los mensajes de éxito y error viajan en `TempData`, de modo que sobreviven a la redirección
posterior a un `POST`. Se muestran como alertas descartables, con `role="status"` para los de éxito y
`role="alert"` para los de error, que es lo que hace que un lector de pantalla los anuncie con la
urgencia adecuada.

## 10. Accesibilidad

| Medida | Para qué |
| ----- | ----- |
| Enlace «Saltar al contenido principal» | Evitar recorrer la navegación con el teclado en cada página. |
| `lang="es-CR"` en el documento | Que el lector de pantalla use la pronunciación correcta. |
| Tablas con `<caption>` oculto visualmente y `<th scope="col">` | Que la estructura sea navegable sin ver la pantalla. |
| `aria-pressed` en los botones de alternancia | Que se anuncie el estado, no solo la acción. |
| Etiquetas asociadas a cada control | Que al pulsar la etiqueta se enfoque el campo y el lector lo anuncie. |
| Colores de Bootstrap 5.3 en ambos temas | Contraste suficiente sin ajustes manuales. |

## 11. Diseño adaptable

Rejilla de Bootstrap. Las tablas anchas van dentro de un contenedor con desplazamiento horizontal
propio (`.tabla-desplazable`), de modo que la página nunca se desplaza en horizontal completa. El
menú se colapsa en pantallas pequeñas.

## 12. Pruebas

Los recorridos se verifican con Playwright sobre un navegador real. Ver
[../pruebas.md](../pruebas.md) §5:

- Recorrido completo desde el registro de proveedores hasta la mejor oferta con su aprobador.
- Rechazo del nombre duplicado y de la fecha de cierre pasada, con el mensaje visible.
- Alternancia de moneda y de tema, esta última conservada al navegar.
- Confirmación antes de eliminar: al cancelar, el registro sigue ahí.

---

[← Volver al índice de documentación](../README.md)
