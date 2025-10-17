# ===========================
# AutoAPI.Builder — EF destekli SDK imajı
# ===========================
FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /src
COPY . .

# ✅ EF CLI yükle (global)
RUN dotnet tool install --global dotnet-ef --version 8.*
ENV PATH="$PATH:/root/.dotnet/tools"

# ✅ Builder projesini derle
WORKDIR "/src/AutoAPI.Builder"
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# ✅ Çalışma dizinini ayarla
WORKDIR /app
COPY /src/AutoAPI.Builder/appsettings.json ./appsettings.json

# ✅ Kaynak kodları EF erişimi için kopyala
COPY . /src

ENTRYPOINT ["dotnet", "/app/publish/AutoAPI.Builder.dll"]
