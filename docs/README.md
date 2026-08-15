# Documentación del Sistema de Gestión de Licitaciones

Esta carpeta es la **única ubicación autorizada** de la documentación del proyecto. Todo está en Markdown y
las imágenes o evidencias se guardan en [`assets/`](assets). Este archivo sustituye al README tradicional de
la raíz del repositorio.

## Puesta en marcha rápida

```bash
# 1. Levantar la solución completa (aplicación + PostgreSQL)
docker compose up --build

# 2. Abrir la aplicación
#    Interfaz web:        http://localhost:8080
#    Documentación API:   http://localhost:8080/swagger
```

Antes del primer arranque hay que crear el archivo de credenciales:

```bash
cp .env.example .env   # y editar POSTGRES_PASSWORD
```

Instrucciones completas en [docker.md](docker.md) y [kubernetes.md](kubernetes.md).

### Sin Docker

Con solo el SDK de .NET 9 se pueden ejecutar la compilación y las pruebas unitarias:

```bash
dotnet build Licitaciones.sln
dotnet test tests/Licitaciones.UnitTests/Licitaciones.UnitTests.csproj
```

Para ejecutar la aplicación contra una instancia propia de PostgreSQL, la cadena de conexión se
configura fuera del repositorio, que no contiene credenciales de ningún entorno:

```bash
dotnet user-secrets --project src/Licitaciones.Web \
  set "ConnectionStrings:Licitaciones" "Host=localhost;Port=5432;Database=licitaciones;Username=...;Password=..."
dotnet run --project src/Licitaciones.Web
```

Las pruebas de integración y las funcionales necesitan Docker en marcha. Ver
[pruebas.md](pruebas.md) §7.

## Índice

### Proceso (Extreme Programming)

| Documento | Contenido |
| ----- | ----- |
| [vision-alcance.md](vision-alcance.md) | Propósito del sistema, alcance incluido y excluido, actores y glosario. |
| [historias-usuario.md](historias-usuario.md) | Las 22 historias con prioridad, estimación y criterios de aceptación. |
| [plan-xp.md](plan-xp.md) | Planning Game, plan de liberación, iteraciones y reglas de trabajo XP. |
| [bitacora-xp.md](bitacora-xp.md) | Resultados por iteración: velocidad, TDD, refactorizaciones y retroalimentación. |
| [uso-ia.md](uso-ia.md) | Declaración del uso de herramientas de inteligencia artificial. |

### Arquitectura y datos

| Documento | Contenido |
| ----- | ----- |
| [arquitectura-general.md](arquitectura-general.md) | Capas, dependencias, decisiones de diseño y diagramas. |
| [modelo-datos.md](modelo-datos.md) | Entidades, relaciones, restricciones e índices. |
| [integracion-modulos.md](integracion-modulos.md) | Cómo cooperan los módulos y flujos de extremo a extremo. |

### Módulos

| Documento | Contenido |
| ----- | ----- |
| [modulos/licitaciones.md](modulos/licitaciones.md) | Ciclo de vida, reglas de estado y presupuesto. |
| [modulos/proveedores.md](modulos/proveedores.md) | Unicidad normalizada y borrado lógico. |
| [modulos/ofertas.md](modulos/ofertas.md) | Registro, validaciones, mejor oferta y clasificación. |
| [modulos/niveles-aprobacion.md](modulos/niveles-aprobacion.md) | Rangos parametrizables sin traslape. |
| [modulos/tipo-cambio.md](modulos/tipo-cambio.md) | Administración del tipo de cambio y conversión CRC/USD. |
| [modulos/interfaz-web.md](modulos/interfaz-web.md) | Navegación, temas, validación y accesibilidad. |
| [modulos/api-rest.md](modulos/api-rest.md) | Contratos HTTP, versionado y errores. |
| [modulos/persistencia.md](modulos/persistencia.md) | Entity Framework Core, PostgreSQL, migraciones y concurrencia. |

### Verificación y operación

| Documento | Contenido |
| ----- | ----- |
| [pruebas.md](pruebas.md) | Estrategia de pruebas, ejecución, cobertura y casos principales. |
| [api.md](api.md) | Endpoints, contratos, ejemplos y colección reproducible de solicitudes. |
| [docker.md](docker.md) | Imagen, Docker Compose, persistencia y comprobaciones de salud. |
| [kubernetes.md](kubernetes.md) | Manifiestos, sondas, almacenamiento y diagnóstico del despliegue. |
| [assets/](assets/README.md) | Recursos de apoyo, incluida la colección reproducible de solicitudes a la API. |

### Estructura del repositorio

```
src/            Licitaciones.{Domain, Application, Infrastructure, Api, Web}
tests/          Licitaciones.{UnitTests, IntegrationTests, FunctionalTests}
docs/           Esta documentación (única ubicación autorizada)
k8s/            Manifiestos de Kubernetes
.github/        Integración continua
Dockerfile      Imagen de la aplicación
docker-compose.yml
```

---

Proyecto final del curso **ITI-822 · Metodologías Ágiles de Desarrollo de Software**,
Universidad Técnica Nacional.
