# ===========================
# 1️⃣ Build aşaması
# ===========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Tüm projeleri container'a kopyala
COPY . .

# Builder projesine geç
WORKDIR "/src/AutoAPI.Builder"

# Restore ve build işlemleri
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# ===========================
# 2️⃣ Runtime aşaması
# ===========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 🔧 Docker CLI erişimi için gerekli paketler
RUN apt-get update && \
    apt-get install -y ca-certificates curl gnupg lsb-release && \
    mkdir -p /etc/apt/keyrings && \
    curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg && \
    echo \
      "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
      https://download.docker.com/linux/debian \
      $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null && \
    apt-get update && \
    apt-get install -y docker-ce-cli && \
    rm -rf /var/lib/apt/lists/*

# ✅ Derlenmiş dosyaları kopyala
COPY --from=build /app/publish .

# ✅ Kaynak kodları da kopyala (dotnet ef için .csproj erişimi)
COPY . /app/src

# ===========================
# 3️⃣ Çalışma dizini ve giriş noktası
# ===========================
WORKDIR /app
ENTRYPOINT ["dotnet", "AutoAPI.Builder.dll"]
