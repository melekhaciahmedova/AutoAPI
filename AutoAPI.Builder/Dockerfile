# ============================
# 1️⃣ BUILD STAGE
# ============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Dotnet global tools klasörünü sistem PATH'ine ekle
ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR /src

# EF CLI'yı global kur
RUN dotnet tool install --global dotnet-ef --version 8.*

# Çözümü kopyala
COPY AutoAPI.sln ./
COPY AutoAPI.Core/ AutoAPI.Core/
COPY AutoAPI.Domain/ AutoAPI.Domain/
COPY AutoAPI.Data/ AutoAPI.Data/
COPY AutoAPI.Builder/ AutoAPI.Builder/

# Restore işlemini tam çözüm düzeyinde yap (tüm projeler restore edilir)
RUN dotnet restore AutoAPI.sln

# Build işlemi
WORKDIR /src/AutoAPI.Builder
RUN dotnet publish -c Release -o /app/publish

# ============================
# 2️⃣ RUNTIME STAGE
# ============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# EF CLI'nın runtime'da da mevcut olması için yeniden ekle
ENV PATH="$PATH:/root/.dotnet/tools"
ENV DOTNET_ROOT=/usr/share/dotnet
ENV ASPNETCORE_ENVIRONMENT=Development

# Build aşamasından çıktı al
COPY --from=build /app/publish .

# Dotnet EF tool'u doğrudan kullanabilmek için tekrar kurulum (cache'ten alır)
RUN dotnet tool install --global dotnet-ef --version 8.* --ignore-failed-sources

ENTRYPOINT ["dotnet", "AutoAPI.Builder.dll"]