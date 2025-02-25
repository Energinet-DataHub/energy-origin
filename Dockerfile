ARG SDK_VERSION
ARG RUNTIME_VERSION
ARG BUILDKIT_SBOM_SCAN_STAGE=true
ARG BUILDKIT_SBOM_SCAN_CONTEXT=true
FROM mcr.microsoft.com/dotnet/aspnet:${RUNTIME_VERSION}-noble-chiseled-extra AS base

FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-noble AS build
ARG PROJECT
ARG MIGRATIONS
ARG BUILDKIT_SBOM_SCAN_STAGE=true
ARG BUILDKIT_SBOM_SCAN_CONTEXT=true
WORKDIR /src/
COPY . .
RUN <<EOR
grep -q "<AssemblyName>" ${PROJECT}
if [ $? -eq 0 ]; then
    sed -i ${PROJECT} -e "s|<AssemblyName>.*</AssemblyName>|<AssemblyName>main</AssemblyName>|"
else
    sed -i ${PROJECT} -e "s|</PropertyGroup>|<AssemblyName>main</AssemblyName></PropertyGroup>|"
fi
EOR
RUN dotnet tool restore || true
RUN dotnet publish ${PROJECT} -c Release -o /app/publish
WORKDIR /app/publish
RUN rm -f appsettings.json appsettings.*.json || true
RUN <<EOR
mkdir /app/migrations
if [ ! -z ${MIGRATIONS} ]; then
    cp -v /src/${MIGRATIONS} /app/migrations
fi
EOR

FROM base AS final
ARG BUILDKIT_SBOM_SCAN_STAGE=true
ARG BUILDKIT_SBOM_SCAN_CONTEXT=true
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /app/migrations /migrations/
COPY --from=busybox:uclibc /bin/cp /bin/cp
COPY --from=busybox:uclibc /bin/cat /bin/cat
COPY --from=busybox:uclibc /bin/ls /bin/ls
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["/app/main"]
