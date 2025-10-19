FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /src

# PATH'i en başta ekle (hem build-time hem runtime için)
ENV PATH="/root/.dotnet/tools:${PATH}"

# EF CLI yükle
RUN dotnet tool install --global dotnet-ef

# Doğrulama
RUN /root/.dotnet/tools/dotnet-ef --version

# Container'ı sürekli açık tut
CMD ["bash", "-lc", "echo '🛠 autoapi-builder ready (with EF CLI)...'; while true; do sleep 3600; done"]