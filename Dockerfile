FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-jammy AS build

ARG SUBSYSTEM
ARG PROJECT

WORKDIR /src/
COPY ${SUBSYSTEM}/ .
WORKDIR /src/${PROJECT}
RUN dotnet restore
RUN dotnet build -c Release --no-restore
RUN dotnet publish -c Release -o /app/publish --no-restore --no-build

FROM mcr.microsoft.com/dotnet/aspnet:${RUNTIME_VERSION}-jammy-chiseled-extra AS final

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
