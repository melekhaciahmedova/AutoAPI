FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /src

RUN dotnet tool install --global dotnet-ef

ENV PATH="$PATH:/root/.dotnet/tools"

CMD ["sh", "-c", "echo '🛠 autoapi-builder ready (with EF CLI)...'; while true; do sleep 3600; done"]