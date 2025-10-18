# -----------------------------------------
# AutoAPI Builder (SDK + EF CLI + Always On)
# -----------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0

# Ortam değişkeni: dotnet tools PATH'i
ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR /src

# EF Core CLI yükle
RUN dotnet tool install --global dotnet-ef

# Versiyonu kontrol et (debug amaçlı)
RUN dotnet ef --version

# Container’ı sürekli açık tut
CMD ["sh", "-c", "echo '🛠 autoapi-builder ready (with EF CLI)...'; while true; do sleep 3600; done"]