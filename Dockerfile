ARG SDK_VERSION
ARG RUNTIME_VERSION
FROM mcr.microsoft.com/dotnet/aspnet:${RUNTIME_VERSION}-jammy-chiseled-extra AS base

FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-jammy AS build
ARG SUBSYSTEM
ARG PROJECT
WORKDIR /src/
RUN ls -la /src
COPY ${SUBSYSTEM}/ .
WORKDIR /src/${PROJECT}
RUN rm -f appsettings.json appsettings.*.json || true
RUN ls -la /src/${PROJECT}
RUN dotnet tool restore
RUN dotnet restore
RUN dotnet dotnet-CycloneDX /src/${PROJECT} -o /app/publish/sbom.xml
RUN dotnet build -c Release --no-restore
RUN dotnet publish -c Release -o /app/publish --no-restore --no-build

FROM base AS final
ARG SUBSYSTEM
WORKDIR /app
RUN ls -la /app
COPY --from=build /app/publish .
COPY --from=build /app/publish/sbom.xml /app/sbom.xml
COPY ${SUBSYSTEM}/migrations/* /migrations/
COPY --from=busybox:uclibc /bin/cp /bin/cp
COPY --from=busybox:uclibc /bin/cat /bin/cat
COPY --from=busybox:uclibc /bin/ls /bin/ls
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["/app/main"]
