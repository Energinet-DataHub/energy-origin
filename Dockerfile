ARG RUNTIME_VERSION
FROM mcr.microsoft.com/dotnet/aspnet:${RUNTIME_VERSION}-jammy-chiseled-extra AS base

ARG SDK_VERSION
FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-jammy AS build
ARG SUBSYSTEM
ARG PROJECT

WORKDIR /src/
COPY ${SUBSYSTEM}/ .

# Switch to the project directory to perform build and publish
WORKDIR /src/${PROJECT}
RUN rm -f appsettings.json appsettings.*.json || true
RUN dotnet tool restore
RUN dotnet restore
RUN dotnet build -c Release --no-restore
RUN dotnet publish -c Release -o /app/publish --no-restore --no-build

# Switch back to the subsystem directory containing the solution file for SBOM generation
WORKDIR /src/${SUBSYSTEM}
RUN dotnet dotnet-CycloneDX . -o /app/publish/sbom.xml -f xml

FROM base AS final
ARG SUBSYSTEM

WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /app/publish/sbom.xml /app/sbom.xml
COPY ${SUBSYSTEM}/migrations/* /migrations/

# Copy essential binaries from busybox for basic file operations.
COPY --from=busybox:uclibc /bin/cp /bin/cp
COPY --from=busybox:uclibc /bin/cat /bin/cat
COPY --from=busybox:uclibc /bin/ls /bin/ls

# Expose ports 8080 and 8081 for the application.
EXPOSE 8080
EXPOSE 8081

# Set the entrypoint for the container.
ENTRYPOINT ["/app/main"]
