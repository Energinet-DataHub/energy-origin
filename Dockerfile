ARG SDK_VERSION
ARG RUNTIME_VERSION
FROM mcr.microsoft.com/dotnet/aspnet:${RUNTIME_VERSION}-jammy-chiseled AS base

FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-1-jammy AS build
ARG SUBSYSTEM
ARG PROJECT
WORKDIR /src/
COPY ${SUBSYSTEM}/ .
WORKDIR /src/${PROJECT}
RUN dotnet restore --runtime linux-x64
RUN dotnet build -c Release --no-restore --runtime linux-x64
RUN dotnet publish -c Release -o /app/publish --no-restore --self-contained true --runtime linux-x64 --no-build

FROM base AS final
ARG SUBSYSTEM
COPY ${SUBSYSTEM}/migrations/${MIGRATION_FILE} /migrations
COPY --from=build /app/publish /app

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["main"]
