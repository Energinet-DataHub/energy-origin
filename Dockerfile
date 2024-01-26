ARG SDK_VERSION
ARG RUNTIME_VERSION
FROM mcr.microsoft.com/dotnet/aspnet:${RUNTIME_VERSION}-jammy-chiseled-extra AS base

FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-jammy AS build
ARG SUBSYSTEM
ARG PROJECT
WORKDIR /src/
COPY ${SUBSYSTEM}/ .
WORKDIR /src/${PROJECT}

# Install necessary tools
RUN apt-get update && apt-get install -y protobuf-compiler

# Set environment variables
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV GRPC_PROTOC_PLUGIN=/usr/local/lib/grpc_csharp_plugin

RUN dotnet tool restore || true
RUN dotnet restore
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
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["/app/main"]
