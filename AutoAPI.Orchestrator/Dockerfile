# ======================
# 1️⃣ Build Stage
# ======================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Tüm solution'u kopyala
COPY . .

# Orchestrator projesine gir
WORKDIR "/src/AutoAPI.Orchestrator"

# Restore
RUN dotnet restore

# Publish (Release modunda)
RUN dotnet publish -c Release -o /app/publish


# ======================
# 2️⃣ Runtime Stage
# ======================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

# -------- Docker CLI Kurulumu --------
RUN apt-get update && \
    apt-get install -y ca-certificates curl gnupg lsb-release && \
    mkdir -p /etc/apt/keyrings && \
    curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg && \
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
      https://download.docker.com/linux/debian $(lsb_release -cs) stable" \
      > /etc/apt/sources.list.d/docker.list && \
    apt-get update && \
    apt-get install -y docker-ce-cli && \
    rm -rf /var/lib/apt/lists/*

# -------- Orchestrator App'i Kopyala --------
COPY --from=build /app/publish .

# Giriş noktası
ENTRYPOINT ["dotnet", "AutoAPI.Orchestrator.dll"]