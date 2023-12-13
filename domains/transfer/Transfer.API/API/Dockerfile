FROM cgr.dev/chainguard/dotnet-runtime AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 443

FROM cgr.dev/chainguard/dotnet-sdk AS build
WORKDIR /src

# Switch to root to copy the project files and set permissions
USER root
COPY . .
# Set permissions for all project files
RUN chown -R nonroot:nonroot /src

# Switch back to app user
USER nonroot

WORKDIR "/src/Transfer.API/API"
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final

WORKDIR /app
COPY --from=publish /app/publish .
COPY migrations /migrations

ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["/usr/bin/dotnet", "API.dll"]
