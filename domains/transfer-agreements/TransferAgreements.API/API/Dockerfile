FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

RUN apt-get -y update && apt-get -y install wget
HEALTHCHECK CMD wget -qO- http://localhost/health > /dev/null || exit 1

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["TransferAgreements.API/API/API.csproj", "TransferAgreements.API/API/"]
RUN dotnet restore "TransferAgreements.API/API/API.csproj"
COPY . .
WORKDIR "/src/TransferAgreements.API/API"
RUN dotnet build "API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "API.dll"]
