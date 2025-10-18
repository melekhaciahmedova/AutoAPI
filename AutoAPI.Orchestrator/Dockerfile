FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY AutoAPI.sln ./
COPY AutoAPI.Builder/ AutoAPI.Builder/
COPY AutoAPI.Core/ AutoAPI.Core/
COPY AutoAPI.Data/ AutoAPI.Data/
COPY AutoAPI.Domain/ AutoAPI.Domain/
RUN dotnet restore "AutoAPI.Builder/AutoAPI.Builder.csproj"
RUN dotnet publish "AutoAPI.Builder/AutoAPI.Builder.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AutoAPI.Builder.dll"]