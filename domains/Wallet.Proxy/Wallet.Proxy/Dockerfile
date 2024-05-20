FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Wallet.Proxy/Wallet.Proxy.csproj", "Wallet.Proxy/"]
RUN dotnet restore "Wallet.Proxy/Wallet.Proxy.csproj"
COPY . .
WORKDIR "/src/Wallet.Proxy"
RUN dotnet build "Wallet.Proxy.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Wallet.Proxy.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Wallet.Proxy.dll"]
