FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["API/Mock/Oidc.Mock.csproj", "API/Mock/"]
RUN dotnet restore "API/Mock/Oidc.Mock.csproj"
COPY . .
WORKDIR "/src/API/Mock"
RUN dotnet build "Oidc.Mock.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Oidc.Mock.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Oidc.Mock.dll"]
