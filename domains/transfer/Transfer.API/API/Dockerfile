ARG SDK_VERSION
ARG RUNTIME_VERSION
FROM cgr.dev/chainguard/dotnet-runtime AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443


FROM cgr.dev/chainguard/dotnet-sdk AS build
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
FROM cgr.dev/chainguard/dotnet:latest AS final
WORKDIR /app

# Copy from publish stage and set permissions
COPY --from=publish --chown=app:app /app/publish .
COPY --chown=app:app migrations /migrations

# Use non-root user
USER app

ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["/usr/bin/dotnet", "API.dll"]
