FROM mcr.microsoft.com/dotnet/sdk:8.0

# Ortam değişkenleri
ENV DOTNET_CLI_HOME=/root \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# PATH değişikliğini kalıcı hale getir
RUN mkdir -p /root/.dotnet/tools \
    && echo 'export PATH=$PATH:/root/.dotnet/tools' >> /root/.bashrc \
    && echo 'export PATH=$PATH:/root/.dotnet/tools' >> /etc/profile

# dotnet-ef global kurulum
RUN dotnet tool install --global dotnet-ef --version 8.* \
    && dotnet ef --version

WORKDIR /src

# Container'ın aktif kalması için
CMD ["bash", "-lc", "echo '🛠 autoapi-builder ready (EF global tool fixed)' && sleep infinity"]