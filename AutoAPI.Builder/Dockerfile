FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /src

COPY . .

RUN dotnet tool install --global dotnet-ef --version 8.*
ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR "/src/AutoAPI.Builder"
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

WORKDIR /app
ENTRYPOINT ["dotnet", "/app/publish/AutoAPI.Builder.dll"]