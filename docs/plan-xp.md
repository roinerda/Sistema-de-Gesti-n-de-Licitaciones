# Plan XP: liberación, iteraciones y reglas de trabajo

Este proyecto se desarrolla **exclusivamente con Extreme Programming (XP)**. No se emplean roles,
ceremonias ni artefactos de Scrum o Kanban: no hay Product Owner, Scrum Master, Sprint Backlog, Daily Scrum,
Sprint Review, Sprint Retrospective, tablero Kanban ni límites WIP.

## 1. Modalidad

| Aspecto | Definición |
| ----- | ----- |
| Integrantes | 1 persona (modalidad individual). |
| Programación en parejas | No aplica por ser trabajo individual. Se sustituye por la práctica compatible descrita en §5. |
| Propiedad del código | Individual por modalidad; ningún módulo queda fuera del conocimiento de la persona autora. |
| Repositorio | Único, en GitHub, con historial incremental. |

## 2. Planning Game

El Planning Game se ejecutó en dos niveles:

**Juego de planificación de la liberación.** A partir del enunciado, que actúa como voz del cliente, se
escribieron las 22 historias de [historias-usuario.md](historias-usuario.md). El cliente fijó la prioridad
según el valor de negocio; la parte técnica estimó en puntos según el esfuerzo. Las historias se ordenaron
en cuatro iteraciones respetando dependencias reales: no se puede evaluar la mejor oferta antes de poder
registrar ofertas, ni registrar ofertas antes de tener licitaciones y proveedores.

**Juego de planificación de cada iteración.** Al inicio de cada iteración, las historias seleccionadas se
descomponen en tareas técnicas, cada tarea empieza por una prueba y el avance real se compara con lo
planificado. El resultado se registra en [bitacora-xp.md](bitacora-xp.md).

## 3. Plan de liberación

| Iteración | Objetivo demostrable | Historias | Puntos | Entregas del enunciado (§14.1) |
| ----- | ----- | ----- | ----- | ----- |
| **1** | Un proveedor puede registrarse, consultarse y eliminarse, y los datos persisten en PostgreSQL. | H-01 … H-05 | 14 | 1, 2, 3, 4, 5 |
| **2** | Una licitación completa su ciclo: se crea, se publica, recibe ofertas y muestra la mejor oferta. | H-06 … H-11 | 17 | 6, 7 |
| **3** | La solución es usable de extremo a extremo desde el navegador y desde la API REST, con aprobación y conversión monetaria. | H-12 … H-17 | 24 | 8, 9, 10, 11 |
| **4** | La solución está verificada, empaquetada, desplegable y documentada. | H-18 … H-22 | 24 | 12, 13, 14, 15 |

Cada iteración cierra con una **pequeña liberación**: una versión ejecutable y demostrable. Las
liberaciones se identifican con etiquetas `iteracion-1` … `iteracion-4`, y la entrega evaluable con
`v1.0.0`.

## 4. Duración de las iteraciones

Las cuatro iteraciones tienen **duración uniforme**. La uniformidad es lo que permite comparar la velocidad
entre iteraciones: si una iteración durara más que otra, los puntos completados no serían comparables.

## 5. Prácticas XP aplicadas

| Práctica | Cómo se aplica en este proyecto |
| ----- | ----- |
| **Planning Game** | Historias con prioridad del cliente, estimación técnica, plan de liberación y plan por iteración (§2 y §3). |
| **Historias de usuario** | 22 historias con criterios de aceptación verificables en [historias-usuario.md](historias-usuario.md). |
| **Iteraciones cortas** | Cuatro iteraciones de duración uniforme, cada una con alcance cerrado. |
| **Pequeñas liberaciones** | Al cierre de cada iteración existe una versión ejecutable y demostrable, etiquetada en el repositorio. |
| **TDD** | Cada regla de negocio se escribe primero como prueba que falla, luego se implementa el mínimo código que la aprueba y después se refactoriza. El detalle por iteración está en [bitacora-xp.md](bitacora-xp.md). |
| **Integración continua** | GitHub Actions compila, prueba, mide cobertura, verifica formato, construye la imagen y valida los manifiestos en cada `push` y `pull request`. Un fallo bloquea la integración. |
| **Diseño simple** | Se implementa solo lo que las historias vigentes exigen. Las exclusiones deliberadas están justificadas en [vision-alcance.md](vision-alcance.md) §4. |
| **Refactorización** | Mejoras continuas de estructura sin cambiar el comportamiento observable, siempre con la batería de pruebas en verde antes y después. Las refactorizaciones realizadas se registran en la bitácora. |
| **Propiedad colectiva** | En modalidad individual, la persona autora es responsable de todo el código y puede modificar cualquier módulo; no existen áreas «de otra persona». |
| **Estándares de código** | `.editorconfig` compartido, análisis estático de .NET habilitado, nombres descriptivos en español, documentación XML y `dotnet format` verificado en integración continua. |
| **Ritmo sostenible** | El trabajo se distribuye a lo largo de las iteraciones; el historial muestra avance incremental por entrega, no una construcción concentrada al final. |
| **Pruebas de aceptación** | Cada historia terminada se verifica con pruebas reproducibles asociadas a sus criterios de aceptación. |
| **Cliente disponible** | El enunciado del curso actúa como voz del cliente. Cuando una regla admite más de una lectura, la decisión tomada se documenta explícitamente en el módulo correspondiente. |

### Programación en parejas en modalidad individual

El enunciado exige programación en parejas solo cuando el equipo tiene dos integrantes. En modalidad
individual se aplican las prácticas compatibles que cumplen la misma función —revisión continua del código
y detección temprana de defectos—:

- **Revisión diferida del propio código**: cada entrega se revisa antes de integrarse, con la batería de
  pruebas en verde.
- **TDD como control permanente**: la prueba escrita primero cumple el papel del segundo par de ojos al
  definir el comportamiento esperado antes de implementarlo.
- **Análisis estático automatizado**: los analizadores de .NET y `dotnet format` actúan como revisor
  mecánico en cada compilación e integración.

## 6. Reglas de trabajo

1. **Ninguna funcionalidad sin prueba.** Si una regla de negocio no tiene prueba, la historia no está
   terminada.
2. **La rama principal siempre compila.** No se integra código que rompa la compilación o las pruebas.
3. **Commits pequeños y con propósito**, siguiendo Conventional Commits en español: `feat`, `fix`, `test`,
   `refactor`, `docs`, `chore`, con el módulo entre paréntesis.
4. **Cada historia se vincula** con sus commits, sus pruebas y su documento de módulo.
5. **La documentación vive en `/docs`** y se actualiza en la misma iteración en que cambia el
   comportamiento; documentación desactualizada se trata como defecto.
6. **Sin secretos en el repositorio.** Credenciales y cadenas de conexión se inyectan por variables de
   entorno o `Secret`.
7. **Refactorizar solo en verde.** Cualquier refactorización parte de una batería de pruebas en verde y
   termina con la batería en verde.

## 7. Definición de terminado

Una historia está terminada cuando cumple **todos** estos puntos:

- [x] Sus criterios de aceptación están cubiertos por pruebas automatizadas que pasan.
- [x] El código compila sin advertencias evitables y respeta el formato del proyecto.
- [x] Está documentada en el archivo de módulo correspondiente dentro de `/docs`.
- [x] Es demostrable en la interfaz web o en la API REST, según corresponda.
- [x] Sus commits referencian la historia.

---

[← Volver al índice de documentación](README.md)
