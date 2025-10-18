# --------------------------------------------------
# AutoAPI.Builder Dockerfile
# Rebuild komutları için SDK tabanlı container
# --------------------------------------------------

FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /src

# Kaynak kodu paylaş (host'tan gelecek)
COPY . .

# Container sürekli açık kalsın (rebuild için)
CMD ["/bin/bash", "-c", "echo '🛠 Builder container is running (SDK ready)'; tail -f /dev/null"]