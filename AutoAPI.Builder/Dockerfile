# Base Image: SDK, EF Core CLI'yı yüklemek için SDK gereklidir.
FROM mcr.microsoft.com/dotnet/sdk:8.0

# 1. Ortam değişkenlerini tek bir ENV ile ayarlayın.
# Bu, .NET araçlarının yükleneceği yeri ve çalıştırılacak yeri belirler.
# DOTNET_CLI_HOME'u ayarlamak, araçların /root dizinine yüklenmesini sağlar.
ENV DOTNET_CLI_HOME=/root \
    PATH="/root/.dotnet/tools:${PATH}" 

WORKDIR /src

# 2. EF Core CLI'yı yükle
# PATH'e eklediğimiz için, bu komut /root/.dotnet/tools altına yüklenecektir.
RUN dotnet tool install --global dotnet-ef --version 8.*

# İSTEĞE BAĞLI: Yüklemenin başarılı olduğunu doğrula
# Bu sefer tam yol olmadan çağırıyoruz.
RUN dotnet ef --version

# 3. Proje dosyalarını kopyala ve geri kalan adımları tamamla
# COPY . .
# RUN dotnet restore
# RUN dotnet publish ...

# Container'ın yaşaması için gerekli komut (Sizin önceki CMD'nizi korur)
CMD ["sh", "-c", "echo '🛠 autoapi-builder ready (EF 8.x installed, PATH fixed)'; while true; do sleep 3600; done"]