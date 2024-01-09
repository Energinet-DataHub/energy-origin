ARG SDK_VERSION
ARG RUNTIME_VERSION
# FIXME: wait for nightly to become released!
FROM mcr.microsoft.com/dotnet/nightly/aspnet:${RUNTIME_VERSION}-jammy-chiseled-extra AS base

FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-1-jammy AS build
ARG SUBSYSTEM
ARG PROJECT
WORKDIR /src/
COPY ${SUBSYSTEM}/ .
WORKDIR /src/${PROJECT}
RUN dotnet tool restore || true
RUN dotnet restore
RUN dotnet build -c Release --no-restore
RUN dotnet publish -c Release -o /app/publish --no-restore --no-build

FROM base AS final
ARG SUBSYSTEM
COPY ${SUBSYSTEM}/migrations/${MIGRATION_FILE} /migrations
COPY --from=build /app/publish /app

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["/app/main"]
