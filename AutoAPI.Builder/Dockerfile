
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ENV PATH="$PATH:/root/.dotnet/tools"
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

WORKDIR /src

# EF CLI kur (global)
RUN dotnet tool install --global dotnet-ef --version 8.*

# Tüm projeleri kopyala
COPY AutoAPI.sln ./
COPY AutoAPI.Core/ AutoAPI.Core/
COPY AutoAPI.Domain/ AutoAPI.Domain/
COPY AutoAPI.Data/ AutoAPI.Data/
COPY AutoAPI.Builder/ AutoAPI.Builder/
COPY AutoAPI.API/ AutoAPI.API/
COPY AutoAPI.Orchestrator/ AutoAPI.Orchestrator/

# Restore işlemi
RUN dotnet restore AutoAPI.sln

# Builder projesini derle ve publish et
WORKDIR /src/AutoAPI.Builder
RUN dotnet build -c Release
RUN dotnet publish AutoAPI.Builder.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

# EF tool klasörünü ve build çıktısını kopyala
COPY --from=build /root/.dotnet /root/.dotnet
COPY --from=build /out .

# Ortam değişkenleri
ENV PATH="$PATH:/root/.dotnet/tools"
ENV DOTNET_ROOT=/usr/share/dotnet
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "AutoAPI.Builder.dll"]