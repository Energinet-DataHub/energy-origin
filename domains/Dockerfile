FROM mcr.microsoft.com/dotnet/sdk:8.0-noble AS build

WORKDIR /app

COPY domains/authorization domains/authorization
COPY domains/certificates domains/certificates

RUN dotnet run --project domains/certificates/Query.API/API/API.csproj --swagger

RUN dotnet run --project domains/authorization/Proxy/Proxy.csproj --swagger

FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-noble-chiseled AS final

WORKDIR /swaggerfiles

COPY --from=build /app/domains/certificates/Query.API/API/contracts.yaml ./contracts.yaml
COPY --from=build /app/domains/authorization/Proxy/proxy.yaml ./proxy.yaml
