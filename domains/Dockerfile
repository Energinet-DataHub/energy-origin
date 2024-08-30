# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0.401-noble AS build

WORKDIR /app

COPY domains/authorization domains/authorization
COPY domains/certificates domains/certificates

RUN dotnet run --project domains/certificates/Query.API/API/API.csproj --swagger
RUN dotnet run --project domains/authorization/Proxy/Proxy.csproj --swagger

# Update stage
FROM ubuntu:noble AS updater

RUN apt-get update && \
    apt-get install -y libc6 libssl3 openssl && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0.8-noble-chiseled AS runtime

# Copy updated packages from updater stage
COPY --from=updater /lib/x86_64-linux-gnu/libc.so.6 /lib/x86_64-linux-gnu/libc.so.6
COPY --from=updater /lib/x86_64-linux-gnu/libcrypto.so.3 /lib/x86_64-linux-gnu/libcrypto.so.3
COPY --from=updater /lib/x86_64-linux-gnu/libssl.so.3 /lib/x86_64-linux-gnu/libssl.so.3
COPY --from=updater /usr/bin/openssl /usr/bin/openssl

# Copy busybox utilities
COPY --from=busybox:1.36.1-uclibc /bin/sh /bin/sh
COPY --from=busybox:1.36.1-uclibc /bin/cp /bin/cp

WORKDIR /app

COPY --from=build /app/domains/authorization/Proxy/proxy.yaml .
COPY --from=build /app/domains/certificates/Query.API/API/contracts.yaml .

ENTRYPOINT ["/bin/sh"]
