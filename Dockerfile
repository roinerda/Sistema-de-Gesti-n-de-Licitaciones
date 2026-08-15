# syntax=docker/dockerfile:1

# =====================================================================================
# Imagen del Sistema de Gestión de Licitaciones.
#
# Construcción en varias etapas: el SDK solo existe mientras se compila y la imagen
# final contiene únicamente el tiempo de ejecución y los archivos publicados. El
# proceso corre con un usuario sin privilegios y el sistema de archivos de la
# aplicación queda de solo lectura.
# =====================================================================================

# ------------------------------------------------------------------ Etapa: restauración
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS restauracion
WORKDIR /origen

# Copiar primero los archivos de proyecto permite que Docker reutilice la capa de
# paquetes mientras no cambien las dependencias, aunque cambie el código.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Licitaciones.Domain/Licitaciones.Domain.csproj src/Licitaciones.Domain/
COPY src/Licitaciones.Application/Licitaciones.Application.csproj src/Licitaciones.Application/
COPY src/Licitaciones.Infrastructure/Licitaciones.Infrastructure.csproj src/Licitaciones.Infrastructure/
COPY src/Licitaciones.Api/Licitaciones.Api.csproj src/Licitaciones.Api/
COPY src/Licitaciones.Web/Licitaciones.Web.csproj src/Licitaciones.Web/

RUN dotnet restore src/Licitaciones.Web/Licitaciones.Web.csproj

# ------------------------------------------------------------------ Etapa: publicación
FROM restauracion AS publicacion
WORKDIR /origen

COPY .editorconfig ./
COPY src/ src/

RUN dotnet publish src/Licitaciones.Web/Licitaciones.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /publicacion \
    -p:UseAppHost=false

# ------------------------------------------------------------------ Etapa: ejecución
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS ejecucion

# curl se instala únicamente para que HEALTHCHECK pueda consultar la sonda de la
# aplicación; la imagen base no trae ninguna herramienta de red.
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm --recursive --force /var/lib/apt/lists/*

WORKDIR /aplicacion
COPY --from=publicacion /publicacion ./

# Puerto no privilegiado: no hace falta ninguna capacidad especial para escucharlo.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0 \
    TZ=America/Costa_Rica

EXPOSE 8080

# APP_UID lo define la imagen base de .NET; corresponde a un usuario sin privilegios.
USER $APP_UID

HEALTHCHECK --interval=15s --timeout=5s --start-period=40s --retries=5 \
    CMD curl --fail --silent --show-error http://localhost:8080/salud/listo || exit 1

ENTRYPOINT ["dotnet", "Licitaciones.Web.dll"]
