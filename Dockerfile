ARG SDK_VERSION
ARG RUNTIME_VERSION
FROM mcr.microsoft.com/dotnet/aspnet:${RUNTIME_VERSION}-jammy-chiseled-extra AS base

FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-jammy AS build
ARG SUBSYSTEM
ARG PROJECT
WORKDIR /src/
COPY ${SUBSYSTEM}/ .
WORKDIR /src/${PROJECT}

RUN apt update && apt install golang -y
RUN GOPATH=/ go install github.com/hweidner/psync@60eef8c

RUN dotnet tool restore || true
RUN dotnet restore
RUN dotnet build -c Release --no-restore
RUN dotnet publish -c Release -o /app/publish --no-restore --no-build

FROM base AS final
ARG SUBSYSTEM
WORKDIR /app
COPY --from=build /app/publish .
COPY ${SUBSYSTEM}/migrations/${MIGRATION_FILE} /migrations
COPY --from=build /bin/psync /bin/psync
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["/app/main"]
