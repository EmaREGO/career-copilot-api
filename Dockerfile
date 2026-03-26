FROM mcr.microsoft.com/playwright/dotnet:v1.58.0-jammy AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CareerCopilot.Api/CareerCopilot.Api.csproj", "CareerCopilot.Api/"]
RUN dotnet restore "CareerCopilot.Api/CareerCopilot.Api.csproj"
COPY . .
WORKDIR "/src/CareerCopilot.Api"
RUN dotnet build "CareerCopilot.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CareerCopilot.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright

ENTRYPOINT ["dotnet", "CareerCopilot.Api.dll"]