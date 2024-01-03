ARG SDK_VERSION
ARG RUNTIME_VERSION
FROM mcr.microsoft.com/dotnet/aspnet:${RUNTIME_VERSION} AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION} AS build
WORKDIR /src
COPY ["ClaimAutomation/Worker/Worker.csproj", "ClaimAutomation/Worker/"]
RUN dotnet restore "ClaimAutomation/Worker/Worker.csproj"
COPY . .
WORKDIR "/src/ClaimAutomation/Worker/"
RUN dotnet build "Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final

RUN apt-get -y update && apt-get -y install wget && apt-get clean

WORKDIR /app
COPY --from=publish /app/publish .
COPY migrations /migrations

ENV ASPNETCORE_HTTP_PORTS=80

ENTRYPOINT ["dotnet", "ClaimAutomation.Worker.dll"]
