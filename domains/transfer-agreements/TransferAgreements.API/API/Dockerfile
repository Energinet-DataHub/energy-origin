ARG SDK_VERSION=7.0.9-bookworm-slim
ARG RUNTIME_VERSION=7.0.9-bookworm-slim
FROM mcr.microsoft.com/dotnet/aspnet:${RUNTIME_VERSION} AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION} AS build
WORKDIR /src
COPY ["TransferAgreements.sln", "."]
COPY ["TransferAgreements.API/API/API.csproj", "TransferAgreements.API/API/"]
COPY ["TransferAgreements.API/Shared/ProjectOriginClients/ProjectOriginClients.csproj", "TransferAgreements.API/Shared/ProjectOriginClients/"]
COPY ["TransferAgreements.API/API.UnitTests/API.UnitTests.csproj", "TransferAgreements.API/API.UnitTests/"]
COPY ["TransferAgreements.API/API.IntegrationTests/API.IntegrationTests.csproj", "TransferAgreements.API/API.IntegrationTests/"]
RUN dotnet restore
COPY . .
RUN dotnet tool restore
RUN dotnet build "TransferAgreements.sln" -c Release

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
RUN apt-get update && apt-get install -y tzdata
WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK CMD wget -qO- http://localhost:80/healthz > /dev/null || exit 1
ENTRYPOINT ["dotnet", "API.dll"]
