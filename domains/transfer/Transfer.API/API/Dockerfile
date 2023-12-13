# Use the .NET SDK image to build the application
FROM cgr.dev/chainguard/dotnet-sdk:latest AS build
WORKDIR /src

# Create directories and set permissions
RUN mkdir -p /src/Transfer.API/API \
    && chown -R app:app /src/Transfer.API

# Switch to non-root user
USER app

# Copy the CSPROJ file and restore any dependencies (via NuGet)
COPY --chown=app:app ["Transfer.API/API/API.csproj", "Transfer.API/API/"]
RUN dotnet restore "Transfer.API/API/API.csproj"

# Copy the rest of the source code
COPY --chown=app:app . .

# Set the working directory to the project directory and build the application
WORKDIR "/src/Transfer.API/API"
RUN dotnet build "API.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
WORKDIR /src/Transfer.API/API
RUN dotnet publish "API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final Stage
FROM cgr.dev/chainguard/dotnet:latest-dev@sha256:21c79e93ad81e41a63b8b04ade4f145138febb04dd78da7b0b1ddbfc1356d3f9 AS final
WORKDIR /app

# Copy from publish stage and set permissions
COPY --from=publish --chown=app:app /app/publish .
COPY --chown=app:app migrations /migrations

# Use non-root user
USER app

ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["/usr/bin/dotnet", "API.dll"]
