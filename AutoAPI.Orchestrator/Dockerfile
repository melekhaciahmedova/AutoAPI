FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

#Containerə lazım olanlar kopyalanır
COPY AutoAPI.sln ./
COPY AutoAPI.Orchestrator/ AutoAPI.Orchestrator/
COPY AutoAPI.Core/ AutoAPI.Core/
COPY AutoAPI.Data/ AutoAPI.Data/
COPY AutoAPI.Domain/ AutoAPI.Domain/
COPY AutoAPI.API/ AutoAPI.API/
RUN dotnet restore "AutoAPI.Orchestrator/AutoAPI.Orchestrator.csproj"
RUN dotnet publish "AutoAPI.Orchestrator/AutoAPI.Orchestrator.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0

RUN apt-get update && apt-get install -y ca-certificates curl gnupg && \
    install -m 0755 -d /etc/apt/keyrings && \
    curl -fsSL https://download.docker.com/linux/debian/gpg -o /etc/apt/keyrings/docker.asc && \
    chmod a+r /etc/apt/keyrings/docker.asc && \
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/debian bookworm stable" > /etc/apt/sources.list.d/docker.list && \
    apt-get update && apt-get install -y docker-ce-cli && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AutoAPI.Orchestrator.dll"]