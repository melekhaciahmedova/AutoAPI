FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY AutoAPI.sln ./
COPY AutoAPI.Core/ AutoAPI.Core/
COPY AutoAPI.Orchestrator/ AutoAPI.Orchestrator/
RUN dotnet restore AutoAPI.Orchestrator/AutoAPI.Orchestrator.csproj
RUN dotnet publish AutoAPI.Orchestrator/AutoAPI.Orchestrator.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Development
ENTRYPOINT ["dotnet", "AutoAPI.Orchestrator.dll"]