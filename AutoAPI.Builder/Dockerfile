FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /src

# Global tool'ların görünmesi için ortam değişkenleri
ENV DOTNET_ROOT="/usr/share/dotnet"
ENV DOTNET_CLI_HOME="/root"
ENV PATH="/root/.dotnet/tools:/usr/share/dotnet:${PATH}"

# Temiz kurulum
RUN rm -rf /root/.dotnet && mkdir -p /root/.dotnet/tools

# EF Core CLI'yı .NET 8 sürümüyle uyumlu şekilde yükle
RUN dotnet tool install --global dotnet-ef --version 8.*

# PATH'in doğru çalıştığını test et
RUN echo $PATH && /root/.dotnet/tools/dotnet-ef --version

# Login shell yerine normal shell ile başlat (PATH bozulmasın)
CMD ["sh", "-c", "echo '🛠 autoapi-builder ready (EF 8.x installed, PATH fixed)'; while true; do sleep 3600; done"]