ARG SDK_VERSION
ARG RUNTIME_VERSION
FROM mcr.microsoft.com/dotnet/aspnet:${RUNTIME_VERSION}-alpine3.17 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-alpine3.17 AS build
WORKDIR /src
COPY ["Auth.API/API/API.csproj", "Auth.API/API/"]
RUN dotnet restore "Auth.API/API/API.csproj"
COPY . .
WORKDIR "/src/Auth.API/API"
RUN dotnet build "API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "API.csproj" -c Release -o /app/publish

FROM base AS final

RUN apk add --no-cache tzdata

WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK CMD wget -qO- http://localhost:80/healthz > /dev/null || exit 1
ENTRYPOINT ["dotnet", "API.dll"]
