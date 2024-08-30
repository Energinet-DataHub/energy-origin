# Use the official .NET SDK image
FROM cgr.dev/chainguard/dotnet-sdk:latest AS build

# Set the working directory
WORKDIR /app

COPY domains/authorization domains/authorization
COPY domains/certificates domains/certificates

# Run the certificates project with --swagger
RUN dotnet run --project domains/certificates/Query.API/API/API.csproj --swagger
# Run the proxy project with --swagger
RUN dotnet run --project domains/authorization/Proxy/Proxy.csproj --swagger
