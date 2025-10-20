FROM mcr.microsoft.com/dotnet/sdk:8.0

# dotnet CLI config
ENV DOTNET_CLI_HOME=/root \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    PATH="/root/.dotnet/tools:$PATH"

WORKDIR /src

# GLOBAL dotnet-ef yükle (manifest değil!)
RUN dotnet tool install --global dotnet-ef --version 8.* \
    && echo "✅ dotnet-ef installed globally"

# global tool dizinini sisteme sabitle
ENV PATH="$PATH:/root/.dotnet/tools"

# test et (build-time'da)
RUN dotnet ef --version || echo "⚠ dotnet ef test failed, will check runtime"

# container'ı aktif tut
CMD ["bash", "-c", "echo '🛠 autoapi-builder ready with dotnet-ef (global tool)' && sleep infinity"]