FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /src

# EF CLI yükle (global olarak)
RUN dotnet tool install --global dotnet-ef

# PATH'i global tools klasörünü içerecek şekilde genişlet
ENV PATH="${PATH}:/root/.dotnet/tools"

# Test amaçlı sürümü yazdır
RUN dotnet ef --version

# Container sürekli açık kalsın
CMD ["sh", "-c", "echo '🛠 autoapi-builder ready (with EF CLI)...'; while true; do sleep 3600; done"]