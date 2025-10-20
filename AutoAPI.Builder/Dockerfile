FROM mcr.microsoft.com/dotnet/sdk:8.0

# Ortam değişkenleri
ENV DOTNET_CLI_HOME=/root \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    PATH="/root/.dotnet/tools:$PATH"

# Çalışma dizini
WORKDIR /src

# dotnet-ef yükle (bash -lc ile PATH doğru olur)
RUN bash -lc "dotnet tool install --global dotnet-ef --version 8.*" \
    && bash -lc "dotnet ef --version"

# Container başlatıldığında PATH’in yüklenmesi için bash -lc kullan
CMD ["bash", "-lc", "echo '🛠 autoapi-builder ready (dotnet-ef global tool fixed)' && sleep infinity"]