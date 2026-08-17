# Declaración del uso de inteligencia artificial

El enunciado (§16) autoriza el uso de herramientas de inteligencia artificial, incluso para realizar
la totalidad del trabajo, siempre que el uso se declare. Esta es la declaración.

## 1. Herramienta utilizada

| Dato | Valor |
| ----- | ----- |
| Herramienta | Claude Code (Anthropic), modelo Claude Opus |
| Modalidad | Agente de línea de comandos con acceso al sistema de archivos, a la terminal y a Git del proyecto |
| Período | 11 y 15 de agosto de 2026 |

## 2. Alcance del uso

La herramienta participó en **todas** las fases del proyecto. Lo escrito abajo es descriptivo, no una
minimización.

| Fase | Participación de la herramienta |
| ----- | ----- |
| Análisis del enunciado | Lectura completa del enunciado y extracción de los requisitos obligatorios en una lista verificable. |
| Documentos XP | Redacción de visión y alcance, las 22 historias de usuario con sus criterios de aceptación, el plan XP y esta bitácora. |
| Diseño de la arquitectura | Propuesta de la separación en capas, del monolito modular y de la composición de la API como biblioteca dentro del host web. |
| Código de dominio y aplicación | Escritura completa de entidades, invariantes, servicios de dominio, casos de uso y DTO. |
| Persistencia | Configuraciones de EF Core, migración inicial, datos semilla, repositorios y unidad de trabajo. |
| Interfaz web y API REST | Controladores, vistas Razor, hojas de estilo, JavaScript y documentación OpenAPI. |
| Pruebas | Las tres suites: unitarias, de integración con Testcontainers y funcionales con Playwright. |
| Empaquetado y despliegue | `Dockerfile`, `docker-compose.yml` y los ocho manifiestos de Kubernetes. |
| Integración continua | El flujo de GitHub Actions y el verificador de umbrales de cobertura. |
| Documentación | Todos los archivos de `/docs`. |
| Historial de Git | Redacción de los mensajes de commit y organización de las entregas incrementales. |

## 3. Aportes de la persona autora

| Aporte | Detalle |
| ----- | ----- |
| Encargo y alcance | Definir qué se construye y aportar el enunciado como voz del cliente. |
| Decisión de entorno | Autorizar la instalación del SDK de .NET 9 en el perfil de usuario. |
| Revisión y aceptación | Revisar el resultado de cada entrega antes de integrarla. |
| Responsabilidad del contenido | La entrega y todo lo que afirma son responsabilidad de la persona autora, incluida la parte generada por la herramienta. |

## 4. Verificación de lo generado

Nada se dio por bueno por venir de la herramienta. Lo que se comprobó de forma automática y
reproducible:

- **Compilación:** `dotnet build Licitaciones.sln` termina con 0 advertencias y 0 errores.
- **Pruebas unitarias:** 172 pruebas en verde, ejecutadas en este equipo.
- **Formato y estilo:** `dotnet format --verify-no-changes` pasa sobre la solución completa.
- **Cobertura:** medida con `coverlet`; 88,19 % de líneas en el dominio y 83,94 % en la capa de
  aplicación solo con las pruebas unitarias.
- **Manifiestos:** validados con un analizador de YAML en este equipo y con `kubeconform` en modo
  estricto en la integración continua.
- **Pruebas de integración y funcionales:** **no se ejecutaron en este equipo** por no haber Docker
  instalado. Se ejecutan en la integración continua, que sí lo tiene, y ahí pasan las 73 de
  integración y los 7 recorridos de navegador.
- **Sistema completo:** la integración continua construye la imagen, comprueba que no corre como
  root, levanta el entorno con Docker Compose, verifica interfaz, API y OpenAPI, y confirma que los
  datos sobreviven a un reinicio de los contenedores.
- **Sin verificar:** el despliegue en un clúster de Kubernetes. Los manifiestos se validan con
  `kubeconform` en modo estricto, pero nunca se aplicaron.

La distinción importa: declarar como verificado algo que no se ejecutó sería falso. Y la primera
ejecución real justificó la cautela — encontró dos defectos que la herramienta había introducido y
que ninguna revisión de escritorio había detectado (§5).

## 5. Defectos que la herramienta introdujo y que las pruebas encontraron

Documentarlos es parte de la declaración honesta del uso.

| Defecto | Cómo se detectó | Corrección |
| ----- | ----- | ----- |
| La clasificación del ahorro usaba el porcentaje redondeado, de modo que un ahorro de ₡1 sobre ₡1 000 000 se rotulaba «Oferta válida sin ahorro» en vez de «Oferta aceptable». | Prueba unitaria escrita para el caso límite. | `fix(ofertas): clasificar el ahorro con el porcentaje exacto` |
| La migración emitía `xmin` como columna real, que PostgreSQL rechaza por ser una columna de sistema. | Generación de la migración. | Columna `version` explícita. |
| La clave foránea del estado de licitación no compilaba por incompatibilidad de tipos entre el enum y el `int` del catálogo. | Compilación. | El catálogo tipa su clave como `EstadoLicitacion`. |
| Cinco vistas Razor con comillas anidadas dentro de un atributo. | Compilación. | Construcción del modelo movida al bloque de código de la vista. |
| `.editorconfig` exigía CRLF mientras el repositorio guarda LF, con lo que la verificación de formato daba resultados distintos en Windows y en Linux. | `dotnet format --verify-no-changes`. | `style(codigo): unificar los finales de línea y la codificación` |
| La regla de nomenclatura exigía guion bajo también a constantes y estáticos de solo lectura. | El mismo comando. | Reglas separadas por tipo de campo. |
| Testcontainers 4.13.0 arrastraba SSH.NET 2025.1.0, con una vulnerabilidad de severidad alta. | `dotnet list package --vulnerable` en la integración continua. | `fix(ci)`: subir a Testcontainers 4.14.0. |
| **La validación de nombres de proveedor era imposible de pasar en el navegador.** El patrón usa clases Unicode y JavaScript solo las reconoce con la marca `u`, que jquery-validation no aplica: el formulario rechazaba cualquier nombre real. | Prueba de navegador, en la primera ejecución real de la suite. | `fix(web)`: método de validación que compila con la marca `u`. |
| **Los errores del servidor eran invisibles en el formulario de licitaciones**, porque el dominio nombra el campo `FechaCierre` y el formulario enlaza `FechaCierreLocal`. | La misma ejecución. | `fix(web)`: si la clave no está enlazada, el mensaje va al resumen. |
| Una prueba afirmaba `409` al eliminar una licitación con ofertas, comportamiento que el sistema no implementa ni debe implementar. | La misma ejecución. | Prueba reescrita para verificar la regla real y documentación alineada. |

## 6. Conclusión

La herramienta escribió la práctica totalidad del código, las pruebas y la documentación, bajo
dirección y revisión de la persona autora. El enunciado permite expresamente esta modalidad y exige
declararla, que es lo que hace este documento.

---

[← Volver al índice de documentación](README.md)
