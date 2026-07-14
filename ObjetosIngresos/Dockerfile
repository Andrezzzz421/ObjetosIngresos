# 1. Capa de ejecución (Base usando .NET 9)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# 2. Capa de compilación (SDK usando .NET 9)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# --- CONFIGURACIONES PARA EVITAR EL STATUS 139 (SEGFAULT) ---
# Desactivar telemetría para ahorrar algo de memoria
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
# Forzar a la compilación a usar recolección de basura de estación de trabajo (usa mucha menos RAM que la de Servidor)
ENV DOTNET_GC_SERVER=0
# Limitar el número de hilos de compilación en paralelo para no saturar la CPU/RAM de Render
ENV MSBUILDDISABLENODEREUSE=1
# ------------------------------------------------------------

# Copiar el archivo de proyecto y restaurar dependencias
COPY ["ObjetosIngresos.csproj", "./"]
RUN dotnet restore "ObjetosIngresos.csproj" 

# Copiar todo el código
COPY . .
WORKDIR "/src/."

# Compilamos limitando los recursos
RUN dotnet build "ObjetosIngresos.csproj" -c Release -o /app/build /p:BuildNodeBundles=false /p:Parallel=false

# 3. Capa de publicación
FROM build AS publish
RUN dotnet publish "ObjetosIngresos.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:BuildNodeBundles=false /p:Parallel=false

# 4. Configuración final del contenedor
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Asegurar que la app en producción no use GC de Servidor si el plan de Render es muy ajustado
ENV DOTNET_GC_SERVER=0
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ObjetosIngresos.dll"]