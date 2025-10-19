FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /src

# PATH'i ekle
ENV PATH="/root/.dotnet/tools:${PATH}"

# EF CLI'yı .NET 8 ile uyumlu sürümde yükle
RUN dotnet tool install --global dotnet-ef --version 8.*

# Doğrudan binary test et
RUN /root/.dotnet/tools/dotnet-ef --version

CMD ["bash", "-lc", "echo '🛠 autoapi-builder ready (with EF 8.x)...'; while true; do sleep 3600; done"]