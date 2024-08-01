# Use the official .NET SDK image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory
WORKDIR /app

COPY . ./

RUN ls -al && pwd
# Run the certificates project with --swagger
RUN dotnet run --project domains/certificates/Query.API/API/API.csproj --swagger
# Run the proxy project with --swagger
RUN dotnet run --project domains/authorization/Proxy/Proxy.csproj --swagger
