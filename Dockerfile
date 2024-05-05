ARG SDK_VERSION
ARG RUNTIME_VERSION
FROM mcr.microsoft.com/dotnet/aspnet:${RUNTIME_VERSION}-jammy-chiseled-extra AS base

FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-jammy AS build
ARG SUBSYSTEM
ARG PROJECT
WORKDIR /src/
COPY ${SUBSYSTEM}/ .
WORKDIR /src/${PROJECT}
RUN rm -f appsettings.json appsettings.*.json || true
RUN dotnet tool restore || true
RUN dotnet restore
RUN dotnet build -c Release --no-restore

RUN dotnet publish -c Release -o /app/publish --no-restore

RUN apt-get update && apt-get install -y curl
RUN curl -sSfL https://raw.githubusercontent.com/anchore/syft/main/install.sh | sh -s -- -b /usr/local/bin

# Generate SBOM using Syft
RUN syft /app/publish -o spdx-json=sbom.spdx.json

FROM base AS final
ARG SUBSYSTEM
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /sbom.spdx.json /app/sbom.spdx.json
COPY ${SUBSYSTEM}/migrations/* /migrations/
COPY --from=busybox:uclibc /bin/cp /bin/cp
COPY --from=busybox:uclibc /bin/cat /bin/cat
COPY --from=busybox:uclibc /bin/ls /bin/ls
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["/app/main"]
