# 1️⃣ Temel SDK image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder

# 2️⃣ dotnet-ef global tool yükle
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

# 3️⃣ Çalışma dizini /src
WORKDIR /src

# 4️⃣ Host’tan projeyi kopyala (eğer volume mount edilmeyecekse)
# COPY . .

# 5️⃣ Gerekiyorsa restore / build adımları
# RUN dotnet restore AutoAPI.sln
# RUN dotnet build AutoAPI.sln -c Release

# 6️⃣ Default davranış
CMD ["bash"]