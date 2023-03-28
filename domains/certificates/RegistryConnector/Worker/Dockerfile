FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

RUN apt-get -y update && apt-get -y install curl
HEALTHCHECK CMD curl --fail http://localhost/health || exit 1

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["RegistryConnector/Worker/Worker.csproj", "RegistryConnector/Worker/"]
RUN dotnet restore "RegistryConnector/Worker/Worker.csproj"
COPY . .
WORKDIR "/src/RegistryConnector/Worker"
RUN dotnet build "Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RegistryConnector.Worker.dll"]
