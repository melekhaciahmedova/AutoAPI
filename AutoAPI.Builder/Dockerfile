FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# EF Core CLI
RUN dotnet tool install --global dotnet-ef --version 8.*
ENV PATH="$PATH:/root/.dotnet/tools"

# Restore only the builder and dependencies
COPY AutoAPI.sln ./
COPY AutoAPI.Core/ AutoAPI.Core/
COPY AutoAPI.Domain/ AutoAPI.Domain/
COPY AutoAPI.Data/ AutoAPI.Data/
COPY AutoAPI.Builder/ AutoAPI.Builder/
RUN dotnet restore AutoAPI.Builder/AutoAPI.Builder.csproj

WORKDIR /src/AutoAPI.Builder
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Development
ENTRYPOINT ["dotnet", "AutoAPI.Builder.dll"]