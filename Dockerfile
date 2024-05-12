ARG SDK_VERSION
ARG RUNTIME_VERSION
FROM mcr.microsoft.com/dotnet/aspnet:${RUNTIME_VERSION}-jammy-chiseled-extra AS base

FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-jammy AS build
ARG PROJECT
WORKDIR /src/
COPY . .

RUN dotnet tool restore || true
RUN dotnet restore "./${PROJECT}"
RUN dotnet build "./${PROJECT}" -c Release --no-restore
RUN dotnet publish "./${PROJECT}" -c Release -o /app/publish --no-restore --no-build
WORKDIR /app/publish
RUN rm -f appsettings.json appsettings.*.json || true

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY migrations/* /migrations/
COPY --from=busybox:uclibc /bin/cp /bin/cp
COPY --from=busybox:uclibc /bin/cat /bin/cat
COPY --from=busybox:uclibc /bin/ls /bin/ls
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["/app/main"]
