ARG SDK_VERSION
ARG RUNTIME_VERSION
FROM mcr.microsoft.com/dotnet/aspnet:${RUNTIME_VERSION} AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION} AS build
WORKDIR /src
COPY ["TransferAgreements.API/API/API.csproj", "TransferAgreements.API/API/"]
RUN dotnet restore "TransferAgreements.API/API/API.csproj"
COPY . .
WORKDIR "/src/TransferAgreements.API/API"
RUN dotnet build "API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final

RUN apt-get -y update && apt-get -y install wget && apt-get clean

WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK CMD wget -qO- http://localhost:80/healthz > /dev/null || exit 1
ENTRYPOINT ["dotnet", "API.dll"]
