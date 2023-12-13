FROM cgr.dev/chainguard/dotnet-runtime AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 443

FROM cgr.dev/chainguard/dotnet-sdk AS build
WORKDIR /src

# Temporarily switch back to root to create directories and set permissions
USER root
RUN mkdir -p /src/Transfer.API/API && \
    chown -R app:app /src/Transfer.API

# Switch back to app user
USER app
COPY --chown=app:app ["Transfer.API/API/API.csproj", "Transfer.API/API/"]
RUN dotnet restore "Transfer.API/API/API.csproj"

COPY . .
WORKDIR "/app/Transfer.API/API"
RUN dotnet build "API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final

WORKDIR /app
COPY --from=publish /app/publish .
COPY migrations /migrations

ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "API.dll"]
