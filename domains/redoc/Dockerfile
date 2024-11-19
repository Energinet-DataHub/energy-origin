FROM mcr.microsoft.com/dotnet/sdk:9.0.100-noble AS builder

WORKDIR /app
COPY domains/authorization domains/authorization
COPY domains/certificates domains/certificates
COPY domains/measurements domains/measurements
COPY domains/libraries domains/libraries

RUN dotnet run --project domains/certificates/Query.API/API/API.csproj --swagger
RUN dotnet run --project domains/authorization/Proxy/Proxy.csproj --swagger
RUN dotnet run --project domains/authorization/Authorization.API/API/API.csproj --swagger
RUN dotnet run --project domains/measurements/Measurements.API/API/API.csproj --swagger

FROM redocly/cli AS packager

WORKDIR /app
COPY --from=builder /app/domains/authorization/Proxy/proxy.yaml .
COPY --from=builder /app/domains/certificates/Query.API/API/contracts.yaml .
COPY --from=builder /app/domains/authorization/Authorization.API/API/authorization.yaml .
COPY --from=builder /app/domains/measurements/Measurements.API/API/measurements.yaml .
COPY domains/redoc/api-info.yaml .
COPY domains/redoc/redoc-template.hbs .

WORKDIR /html
RUN redocly join /app/api-info.yaml /app/authorization.yaml /app/measurements.yaml /app/contracts.yaml /app/proxy.yaml -o /app/openapi.yaml
RUN redocly build-docs /app/openapi.yaml -t /app/redoc-template.hbs --output=/html/index.html

FROM nginxinc/nginx-unprivileged:alpine-slim AS final

COPY domains/redoc/health.html /usr/share/nginx/html/health/index.html
COPY domains/redoc/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=packager /html/index.html /usr/share/nginx/html/
