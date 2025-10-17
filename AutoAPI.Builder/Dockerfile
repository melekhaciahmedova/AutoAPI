# ===========================
# AutoAPI.Builder — EF destekli SDK imajı (son hali)
# ===========================
FROM mcr.microsoft.com/dotnet/sdk:8.0

# 1️⃣ Çalışma dizinini ayarla
WORKDIR /src

# 2️⃣ Tüm projeyi kopyala
COPY . .

# 3️⃣ EF CLI yükle (global)
RUN dotnet tool install --global dotnet-ef --version 8.*
ENV PATH="$PATH:/root/.dotnet/tools"

# 4️⃣ Builder projesini derle ve yayınla
WORKDIR "/src/AutoAPI.Builder"
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# 5️⃣ Çalışma dizinini değiştir
WORKDIR /app

# 6️⃣ EF CLI’nin .csproj dosyalarına erişebilmesi için kaynak kodları da içeri al
COPY . /src

# 7️⃣ Worker servisi başlat
ENTRYPOINT ["dotnet", "/app/publish/AutoAPI.Builder.dll"]
