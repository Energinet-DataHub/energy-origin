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

RUN for proj in $(find . -name '*.csproj'); do dotnet dotnet-CycloneDX "$proj" -o /app/publish/sbom; done

RUN dotnet publish -c Release -o /app/publish --no-restore

FROM busybox AS sbom-stage
COPY --from=build /app/publish/sbom/bom.xml /app/bom.xml
RUN SBOM_CONTENTS=$(base64 -w0 /app/bom.xml)
LABEL org.opencontainers.image.description=$SBOM_CONTENTS

FROM base AS final
ARG SUBSYSTEM
WORKDIR /app
COPY --from=build /app/publish .
COPY ${SUBSYSTEM}/migrations/* /migrations/
COPY --from=busybox:uclibc /bin/cp /bin/cp
COPY --from=busybox:uclibc /bin/cat /bin/cat
COPY --from=busybox:uclibc /bin/ls /bin/ls
LABEL org.opencontainers.image.description=${SBOM_CONTENTS}
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["/app/main"]
