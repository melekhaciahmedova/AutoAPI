# ----------------------------
# AutoAPI Builder (SDK + daima açık)
# ----------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /src
SHELL ["/bin/bash", "-c"]

# Not: Kaynak kodu COPY etmiyoruz. /src host'tan mount edilecek.

# Asla kapanmaması için sonsuz döngü
ENTRYPOINT ["bash", "-c", "echo '🛠 autoapi-builder ready (SDK installed)'; while true; do sleep 3600; done"]