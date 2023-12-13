FROM cgr.dev/chainguard/dotnet-runtime AS base
USER root
WORKDIR /app
EXPOSE 8080
EXPOSE 443

FROM cgr.dev/chainguard/dotnet-sdk AS build
WORKDIR /src

USER root
COPY . .

WORKDIR "/src/Transfer.API/API"
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM build AS publish
USER root
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY migrations /migrations

ENV ASPNETCORE_HTTP_PORTS=8080
USER nonroot
ENTRYPOINT ["/usr/bin/dotnet", "API.dll"]
