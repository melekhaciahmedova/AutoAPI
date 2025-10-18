# -------------------------------
# AutoAPI.Orchestrator Dockerfile
# -------------------------------

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Solution ve projeleri kopyala
COPY AutoAPI.sln ./
COPY AutoAPI.Orchestrator/ AutoAPI.Orchestrator/
COPY AutoAPI.Core/ AutoAPI.Core/
COPY AutoAPI.Data/ AutoAPI.Data/
COPY AutoAPI.Domain/ AutoAPI.Domain/

RUN dotnet restore "AutoAPI.Orchestrator/AutoAPI.Orchestrator.csproj"
RUN dotnet publish "AutoAPI.Orchestrator/AutoAPI.Orchestrator.csproj" -c Release -o /app/publish

# Runtime imaj
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AutoAPI.Orchestrator.dll"]