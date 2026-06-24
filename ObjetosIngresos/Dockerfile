# 1. Capa de ejecución base (Entorno liviano para correr la app)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# 2. Capa de compilación (SDK de .NET 9)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Instalar Node.js dentro de la nube de Render para que Tailwind pueda compilar sus estilos
RUN apt-get update && apt-get install -y curl \
    && curl -sL https://deb.nodesource.com/setup_18.x | bash - \
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/*

# Copiar archivos de configuración de .NET y Node.js primero
COPY ["ObjetosIngresos.csproj", "./"]
COPY ["package.json", "package-lock.json*", "./"]

# Restaurar paquetes de .NET e instalar módulos de Node nativos de Linux
RUN dotnet restore "ObjetosIngresos.csproj"
RUN npm cache clean --force && npm install --include=optional

# Copiar el resto del código fuente
COPY . .
WORKDIR "/src/."

# Render ejecutará esto. Como su entorno es Linux nativo, Tailwind compilará el CSS sin errores
RUN dotnet build "ObjetosIngresos.csproj" -c Release -o /app/build

# 3. Capa de publicación
FROM build AS publish
RUN dotnet publish "ObjetosIngresos.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Configuración final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Asegurar el puerto que Render espera escuchar
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ObjetosIngresos.dll"]