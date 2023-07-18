ARG SDK_VERSION
ARG RUNTIME_VERSION
FROM mcr.microsoft.com/dotnet/aspnet:${RUNTIME_VERSION}-alpine3.17 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-alpine3.17 AS build
WORKDIR /src
COPY ["TransferAgreements.sln", "."]
COPY ["TransferAgreements.API/API/API.csproj", "TransferAgreements.API/API/"]
COPY ["TransferAgreements.API/Shared/ProjectOriginClients/ProjectOriginClients.csproj", "TransferAgreements.API/Shared/ProjectOriginClients/"]
RUN dotnet restore
COPY . .
RUN dotnet tool restore
RUN dotnet build "TransferAgreements.sln" -c Release

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final

RUN apk add --no-cache tzdata

WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK CMD wget -qO- http://localhost:80/healthz > /dev/null || exit 1
ENTRYPOINT ["dotnet", "API.dll"]
