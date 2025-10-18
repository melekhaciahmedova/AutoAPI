# ============================
# 1️⃣ BUILD STAGE
# ============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Global tool klasörünü PATH'e ekle
ENV PATH="$PATH:/root/.dotnet/tools"
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

WORKDIR /src

# EF Tool kurulumu
RUN dotnet tool install --global dotnet-ef --version 8.*

# Çözüm dosyası ve tüm projeleri kopyala
COPY AutoAPI.sln ./
COPY AutoAPI.Core/ AutoAPI.Core/
COPY AutoAPI.Domain/ AutoAPI.Domain/
COPY AutoAPI.Data/ AutoAPI.Data/
COPY AutoAPI.Builder/ AutoAPI.Builder/
COPY AutoAPI.API/ AutoAPI.API/
COPY AutoAPI.Orchestrator/ AutoAPI.Orchestrator/

# Tüm projeler için restore
RUN dotnet restore AutoAPI.sln

# Builder projesini publish et (test amaçlı)
WORKDIR /src/AutoAPI.Builder
RUN dotnet publish -c Release -o /app/publish

# ============================
# 2️⃣ RUNTIME STAGE
# ============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# EF tool'u runtime ortamına da ekle
ENV PATH="$PATH:/root/.dotnet/tools"
ENV DOTNET_ROOT=/usr/share/dotnet
ENV ASPNETCORE_ENVIRONMENT=Development

COPY --from=build /app/publish .

# EF Tool tekrar yüklenir (cache varsa skip eder)
RUN dotnet tool install --global dotnet-ef --version 8.* --ignore-failed-sources

ENTRYPOINT ["dotnet", "AutoAPI.Builder.dll"]