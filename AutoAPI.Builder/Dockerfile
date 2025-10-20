FROM mcr.microsoft.com/dotnet/sdk:8.0

# Ortam değişkenleri
ENV DOTNET_CLI_HOME=/root \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Çalışma dizini
WORKDIR /src

# EF global tool'u kur ve PATH'e ekle
RUN dotnet tool install --global dotnet-ef --version 8.* && \
    export PATH="$PATH:/root/.dotnet/tools" && \
    echo 'export PATH=$PATH:/root/.dotnet/tools' >> /root/.bashrc && \
    dotnet ef --version

# PATH'i kalıcı hale getir
ENV PATH="$PATH:/root/.dotnet/tools"

# Container'ı açık tut
CMD ["bash", "-lc", "echo '🛠 autoapi-builder ready (dotnet-ef fixed permanently)' && sleep infinity"]