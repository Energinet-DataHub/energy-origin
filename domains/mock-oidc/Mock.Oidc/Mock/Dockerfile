FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Mock/Mock.Oidc.csproj", "Mock/"]
RUN dotnet restore "Mock/Mock.Oidc.csproj"
COPY . .
WORKDIR "/src/Mock"
RUN dotnet build "Mock.Oidc.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Mock.Oidc.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK CMD curl --fail http://localhost:5000/health || exit 1
ENTRYPOINT ["dotnet", "Mock.Oidc.dll"]
