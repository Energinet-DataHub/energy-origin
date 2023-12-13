ARG SDK_VERSION
ARG RUNTIME_VERSION
FROM cgr.dev/chainguard/dotnet-runtime AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 443

FROM cgr.dev/chainguard/dotnet-sdk AS build
WORKDIR /app
COPY ["Transfer.API/API/API.csproj", "Transfer.API/API/"]
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
