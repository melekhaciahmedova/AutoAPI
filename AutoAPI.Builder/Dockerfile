# =======================================================
# 🧱 BUILD STAGE
# =======================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .

RUN dotnet restore
RUN dotnet publish AutoAPI.Builder/AutoAPI.Builder.csproj -c Release -o /app/publish

# =======================================================
# 🚀 FINAL STAGE (SDK ortamı)
# =======================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app

RUN apt-get update && \
    apt-get install -y ca-certificates curl gnupg && \
    install -m 0755 -d /etc/apt/keyrings && \
    curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg && \
    echo "deb [arch=amd64 signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/debian bookworm stable" > /etc/apt/sources.list.d/docker.list && \
    apt-get update && \
    apt-get install -y docker-ce-cli && \
    rm -rf /var/lib/apt/lists/*


# ✅ EF CLI'yı global tool olarak kur ve kalıcı PATH'e ekle
RUN dotnet tool install --global dotnet-ef --version 8.*
ENV PATH="$PATH:/root/.dotnet/tools"

# ✅ Hem publish edilmiş Builder hem tüm kaynak kodları taşı
COPY --from=build /app/publish .
COPY --from=build /src /src

ENTRYPOINT ["dotnet", "AutoAPI.Builder.dll"]