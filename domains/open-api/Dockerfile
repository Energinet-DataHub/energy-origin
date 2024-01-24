# Base Image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env

# Install Node.js and npm
RUN apt-get update && \
    apt-get install -y nodejs npm && \
    npm install -g npm@latest

# Install Redocly CLI
RUN npm install -g @redocly/cli

# Copy csproj and restore as distinct layers
WORKDIR /src
COPY ["open-api.csproj", "./"]
RUN dotnet restore "open-api.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "open-api.csproj" -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build-env /app/publish .
ENTRYPOINT ["dotnet", "open-api.dll"]
