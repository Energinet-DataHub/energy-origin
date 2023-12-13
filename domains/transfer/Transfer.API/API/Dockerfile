FROM cgr.dev/chainguard/dotnet-runtime AS base
WORKDIR /app
EXPOSE 8080 443

FROM cgr.dev/chainguard/dotnet-sdk AS build
USER root
WORKDIR /src
COPY . .
WORKDIR "/src/Transfer.API/API"
RUN dotnet restore \
    && dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
COPY --from=publish /app/publish .
COPY migrations /migrations
USER nonroot
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["/usr/bin/dotnet", "API.dll"]
