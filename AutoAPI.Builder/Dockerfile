# ============================
# 1️⃣ BUILD STAGE
# ============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ENV PATH="$PATH:/root/.dotnet/tools"
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

WORKDIR /src

# EF CLI kur (global)
RUN dotnet tool install --global dotnet-ef --version 8.*

# Çözüm ve projeleri kopyala
COPY AutoAPI.sln ./
COPY AutoAPI.Core/ AutoAPI.Core/
COPY AutoAPI.Domain/ AutoAPI.Domain/
COPY AutoAPI.Data/ AutoAPI.Data/
COPY AutoAPI.Builder/ AutoAPI.Builder/
COPY AutoAPI.API/ AutoAPI.API/
COPY AutoAPI.Orchestrator/ AutoAPI.Orchestrator/

# Tüm çözümü restore et
RUN dotnet restore AutoAPI.sln

# Builder projesini build et
WORKDIR /src/AutoAPI.Builder
RUN dotnet publish -c Release -o /app/publish

# ============================
# 2️⃣ RUNTIME STAGE
# ============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

# EF Tool klasörünü build'ten kopyala
COPY --from=build /root/.dotnet /root/.dotnet
COPY --from=build /app/publish .

# Ortam değişkenleri
ENV PATH="$PATH:/root/.dotnet/tools"
ENV DOTNET_ROOT=/usr/share/dotnet
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "AutoAPI.Builder.dll"]