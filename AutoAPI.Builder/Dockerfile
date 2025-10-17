# ===========================
# AutoAPI.Builder — tek aşamalı SDK imajı
# ===========================
FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /src
COPY . .

# EF CLI kurulumu (global)
RUN dotnet tool install --global dotnet-ef --version 8.*
ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR "/src/AutoAPI.Builder"
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

WORKDIR /app
COPY --from=0 /src/AutoAPI.Builder/appsettings.json ./

# Kaynak kodları da içeri al (EF için .csproj erişimi)
COPY . /src

ENTRYPOINT ["dotnet", "/app/publish/AutoAPI.Builder.dll"]