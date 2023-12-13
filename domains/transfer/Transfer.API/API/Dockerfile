ARG SDK_VERSION
ARG RUNTIME_VERSION
FROM cgr.dev/chainguard/dotnet-runtime AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM cgr.dev/chainguard/dotnet-sdk AS build
WORKDIR /src

RUN addgroup -S app && adduser -S app -G app

RUN mkdir -p /src/Transfer.API/API \
    && chown -R app:app /src/Transfer.API

USER app

COPY --chown=app:app ["Transfer.API/API/API.csproj", "Transfer.API/API/"]
RUN dotnet restore "Transfer.API/API/API.csproj"

COPY --chown=app:app . .

WORKDIR "/src/Transfer.API/API"
RUN dotnet build "API.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR /src/Transfer.API/API
RUN dotnet publish "API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish --chown=app:app /app/publish .
COPY --chown=app:app migrations /migrations

USER app

ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["/usr/bin/dotnet", "API.dll"]
