FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /src

# PATH'i önce ekle
ENV PATH="$PATH:/root/.dotnet/tools"

# Sonra EF CLI'yı yükle
RUN dotnet tool install --global dotnet-ef

# Test amaçlı sürümü yazdır (artık görünür)
RUN dotnet ef --version

CMD ["sh", "-c", "echo '🛠 autoapi-builder ready (with EF CLI)...'; while true; do sleep 3600; done"]