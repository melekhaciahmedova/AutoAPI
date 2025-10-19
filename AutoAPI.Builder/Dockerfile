FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /src
ENV PATH="/root/.dotnet/tools:${PATH}"

# Eski dotnet-ef’yi sıfırlamak için .dotnet klasörünü tamamen temizle
RUN rm -rf /root/.dotnet && mkdir -p /root/.dotnet/tools

# Doğrudan EF Core 8.x yükle
RUN dotnet tool install --global dotnet-ef --version 8.*

RUN /root/.dotnet/tools/dotnet-ef --version

CMD ["bash", "-lc", "echo '🛠 autoapi-builder ready (EF 8.x installed)'; while true; do sleep 3600; done"]
