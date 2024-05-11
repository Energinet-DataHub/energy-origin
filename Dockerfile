ARG SDK_VERSION
ARG RUNTIME_VERSION
FROM mcr.microsoft.com/dotnet/aspnet:${RUNTIME_VERSION}-jammy-chiseled-extra AS base

FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-jammy AS prepare-restore-files

ARG SUBSYSTEM
ARG PROJECT
WORKDIR /src/
COPY ${SUBSYSTEM}/ .
WORKDIR /src/${PROJECT}
RUN rm -f appsettings.json appsettings.*.json || true
RUN dotnet tool restore || true
RUN dotnet-subset restore --root-directory /src/${PROJECT} --output-directory /src/${PROJECT}/restore-subset

FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-jammy AS build
ARG SUBSYSTEM
ARG PROJECT

WORKDIR /src
COPY --from=prepare-restore-files /src/${PROJECT}/restore-subset .
RUN dotnet restore ${PROJECT}/restore-subset/${PROJECT}.csproj

RUN dotnet build -c Release --no-restore
RUN dotnet publish -c Release -o /app/publish --no-restore --no-build

FROM base AS final
ARG SUBSYSTEM
WORKDIR /app
COPY --from=build /app/publish .
COPY ${SUBSYSTEM}/migrations/* /migrations/
COPY --from=busybox:uclibc /bin/cp /bin/cp
COPY --from=busybox:uclibc /bin/cat /bin/cat
COPY --from=busybox:uclibc /bin/ls /bin/ls
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["/app/main"]
