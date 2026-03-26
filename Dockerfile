FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiamos todo el contenido de la raíz
COPY . ./
RUN dotnet restore

# IMPORTANTE: Aquí le decimos que el proyecto está dentro de la carpeta
RUN dotnet publish CareerCopilot.Api/CareerCopilot.Api.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Instalación de dependencias de Playwright
RUN apt-get update && apt-get install -y libgbm1 libasound2 libnss3 libxss1 libatk1.0-0 libatk-bridge2.0-0 libcups2 libdrm2 libxcomposite1 libxdamage1 libxrandr2 libpango-1.0-0 libcairo2 libxkbcommon0 && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CareerCopilot.Api.dll"]