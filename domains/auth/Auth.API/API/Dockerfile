FROM mcr.microsoft.com/dotnet/aspnet:6.0.7 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0.302 AS build
WORKDIR /src
COPY ["Auth.API/API/API.csproj", "Auth.API/API/"]
RUN dotnet restore "Auth.API/API/API.csproj"
COPY . .
WORKDIR "/src/Auth.API/API"
RUN dotnet build "API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK CMD curl --fail http://localhost:5000/health || exit 1
ENTRYPOINT ["dotnet", "API.dll"]
