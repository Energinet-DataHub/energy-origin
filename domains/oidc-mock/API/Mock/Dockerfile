ARG SDK_VERSION
ARG RUNTIME_VERSION
FROM mcr.microsoft.com/dotnet/aspnet:${RUNTIME_VERSION}-alpine3.18 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-alpine3.18 AS build
WORKDIR /src
COPY ["API/Mock/Oidc.Mock.csproj", "API/Mock/"]
RUN dotnet restore "API/Mock/Oidc.Mock.csproj"
COPY . .
WORKDIR "/src/API/Mock"
RUN dotnet build "Oidc.Mock.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Oidc.Mock.csproj" -c Release -o /app/publish

FROM base AS final

RUN apk add --no-cache tzdata

WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_HTTP_PORTS=80

ENTRYPOINT ["dotnet", "Oidc.Mock.dll"]
