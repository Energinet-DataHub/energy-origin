FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["RegistryConnector/RegistryConnector/RegistryConnector.csproj", "RegistryConnector/RegistryConnector/"]
RUN dotnet restore "RegistryConnector/RegistryConnector/RegistryConnector.csproj"
COPY . .
WORKDIR "/src/RegistryConnector/RegistryConnector"
RUN dotnet build "RegistryConnector.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RegistryConnector.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RegistryConnector.dll"]
