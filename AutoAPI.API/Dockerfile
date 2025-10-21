FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY AutoAPI.sln ./
COPY AutoAPI.API/ AutoAPI.API/
COPY AutoAPI.Core/ AutoAPI.Core/
COPY AutoAPI.Data/ AutoAPI.Data/
COPY AutoAPI.Domain/ AutoAPI.Domain/
RUN dotnet restore AutoAPI.API/AutoAPI.API.csproj
RUN dotnet publish AutoAPI.API/AutoAPI.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AutoAPI.API.dll"]