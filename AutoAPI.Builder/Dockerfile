FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /src

# Dotnet global tools path ve CLI home'u tanımla
ENV PATH="/root/.dotnet/tools:${PATH}"
ENV DOTNET_ROOT="/usr/share/dotnet"
ENV DOTNET_CLI_HOME="/root"

# Eski tool klasörünü sıfırla
RUN rm -rf /root/.dotnet && mkdir -p /root/.dotnet/tools

# EF Core CLI yükle (NET 8 uyumlu sürüm)
RUN dotnet tool install --global dotnet-ef --version 8.*

# CLI'nın gerçekten çalıştığını doğrula
RUN /root/.dotnet/tools/dotnet-ef --version

# Login shell ortamında başlat (ENV değişkenleri aktif olsun)
CMD ["bash", "-lc", "echo '🛠 autoapi-builder ready (with EF 8.x)...'; while true; do sleep 3600; done"]