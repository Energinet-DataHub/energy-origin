# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory
WORKDIR /app

COPY domains/authorization domains/authorization
COPY domains/certificates domains/certificates

# Run the certificates project with --swagger
RUN dotnet run --project domains/certificates/Query.API/API/API.csproj --swagger
# Run the proxy project with --swagger
RUN dotnet run --project domains/authorization/Proxy/Proxy.csproj --swagger

# Use a chiseled Ubuntu image for the final stage
FROM cgr.dev/chainguard/aspnet-runtime:latest-dev AS runtime

## Install necessary BusyBox tools
#COPY --from=busybox:1.36.1-uclibc /bin/sh /bin/sh
#COPY --from=busybox:1.36.1-uclibc /bin/cp /bin/cp

# Set the working directory
WORKDIR /app

# Copy the Swagger files from the build stage
COPY --from=build /app/domains/authorization/Proxy/proxy.yaml .
COPY --from=build /app/domains/certificates/Query.API/API/contracts.yaml .

# Set the entry point
ENTRYPOINT ["/bin/sh"]
