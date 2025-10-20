# 1️⃣ .NET 8 SDK tabanı
FROM mcr.microsoft.com/dotnet/sdk:8.0

# 2️⃣ Ortam değişkenleri
ENV DOTNET_CLI_HOME=/root \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    PATH="$PATH:/root/.dotnet/tools"

# 3️⃣ Çalışma dizini
WORKDIR /src

# 4️⃣ dotnet-ef global tool’u kalıcı olarak yükle
RUN dotnet new tool-manifest && \
    dotnet tool install dotnet-ef --version 8.* && \
    dotnet tool restore && \
    dotnet ef --version

# 5️⃣ Container'ın aktif kalması için basit bir bekleme
CMD ["bash", "-c", "echo '🛠 autoapi-builder ready (EF 8.x installed & PATH fixed)' && tail -f /dev/null"]