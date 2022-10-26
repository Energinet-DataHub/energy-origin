FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /src
COPY ["Issuer/Issuer.Worker/Issuer.Worker.csproj", "Issuer/Issuer.Worker/"]
RUN dotnet restore "Issuer/Issuer.Worker/Issuer.Worker.csproj"
COPY . .
WORKDIR "/src/Issuer/Issuer.Worker"
RUN dotnet build "Issuer.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Issuer.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Issuer.Worker.dll"]
