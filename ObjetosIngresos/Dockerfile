# 1. Capa de ejecución (Base usando .NET 9)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# 2. Capa de compilación (SDK usando .NET 9)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar el archivo de proyecto y restaurar dependencias
COPY ["ObjetosIngresos.csproj", "./"]
RUN dotnet restore "ObjetosIngresos.csproj" 
# Copiar todo el código y compilar
COPY . .
WORKDIR "/src/."

# Compilamos deshabilitando cualquier intento automático de ejecutar Node/Tailwind en el servidor
RUN dotnet build "ObjetosIngresos.csproj" -c Release -o /app/build /p:BuildNodeBundles=false

# 3. Capa de publicación
FROM build AS publish
RUN dotnet publish "ObjetosIngresos.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:BuildNodeBundles=false

# 4. Configuración final del contenedor
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Asegurar que ASP.NET Core escuche en el puerto que Render espera
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ObjetosIngresos.dll"]
