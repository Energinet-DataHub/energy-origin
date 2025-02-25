ARG SDK_VERSION
ARG RUNTIME_VERSION
FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-noble AS build
ARG PROJECT
ARG MIGRATIONS
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

FROM build AS sbom
WORKDIR /src/
ARG SYFT_RELEASE=1.13.0
ARG SYFT_SHA256=65dd788271d8789e713fbef92464ab8ed01abb12643ad7d0f88af19df60c6bf3
RUN curl -sLO https://github.com/anchore/syft/releases/download/v${SYFT_RELEASE}/syft_${SYFT_RELEASE}_linux_amd64.deb && \
    echo "${SYFT_SHA256} syft_${SYFT_RELEASE}_linux_amd64.deb" | sha256sum --check --status && \
    dpkg -i syft_${SYFT_RELEASE}_linux_amd64.deb && \
    rm syft_${SYFT_RELEASE}_linux_amd64.deb
ARG CycloneDXCLIVersion=0.27.1
RUN curl -LO https://github.com/CycloneDX/cyclonedx-cli/releases/download/v${CycloneDXCLIVersion}/cyclonedx-linux-x64 && \
    chmod +x cyclonedx-linux-x64 && \
    mv cyclonedx-linux-x64 /usr/local/bin/cyclonedx && \
    cyclonedx --version
RUN mkdir -p /app && \
    dotnet tool install --global CycloneDX || true && \
    /root/.dotnet/tools/dotnet-CycloneDX ${PROJECT} -o /app/sbom.xml && \
    syft /src -o cyclonedx-xml=/app/docker-sbom.xml && \
    cyclonedx merge --input-files /app/sbom.xml /app/docker-sbom.xml --output-file /app/combined-sbom.xml

FROM mcr.microsoft.com/dotnet/aspnet:${RUNTIME_VERSION}-noble-chiseled-extra AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /app/migrations /migrations/
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["/app/main"]

FROM sbom AS export-sbom
RUN cp /app/combined-sbom.xml /combined-sbom.xml
