# -------------------------------
# Build aşaması
# -------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY AutoAPI.sln ./
COPY AutoAPI.Orchestrator/ AutoAPI.Orchestrator/
COPY AutoAPI.Core/ AutoAPI.Core/
COPY AutoAPI.Data/ AutoAPI.Data/
COPY AutoAPI.Domain/ AutoAPI.Domain/
RUN dotnet restore "AutoAPI.Orchestrator/AutoAPI.Orchestrator.csproj"
RUN dotnet publish "AutoAPI.Orchestrator/AutoAPI.Orchestrator.csproj" -c Release -o /app/publish

# -------------------------------
# Runtime (docker CLI + runtime)
# -------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Docker CLI’yı kur
RUN apt-get update && apt-get install -y docker-cli && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AutoAPI.Orchestrator.dll"]