# ETAPA DE BASE 
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# dependencias del sistema y el navegador
RUN apt-get update && apt-get install -y \
    wget \
    gnupg \
    && apt-get clean && rm -rf /var/lib/apt/lists/*
# -----------------------------------------------

# ETAPA DE CONSTRUCCIÓN (SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CareerCopilot.Api/CareerCopilot.Api.csproj", "CareerCopilot.Api/"]
RUN dotnet restore "CareerCopilot.Api/CareerCopilot.Api.csproj"
COPY . .
WORKDIR "/src/CareerCopilot.Api"
RUN dotnet build "CareerCopilot.Api.csproj" -c Release -o /app/build

# ETAPA DE PUBLICACIÓN
FROM build AS publish
RUN dotnet publish "CareerCopilot.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ETAPA FINAL (Donde corre la app)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN dotnet tool install --global Microsoft.Playwright.CLI
ENV PATH="$PATH:/root/.dotnet/tools"
RUN playwright install --with-deps chromium

ENTRYPOINT ["dotnet", "CareerCopilot.Api.dll"]