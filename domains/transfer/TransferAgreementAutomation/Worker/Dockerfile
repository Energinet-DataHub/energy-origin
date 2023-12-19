FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TransferAgreementAutomation/Worker/Worker.csproj", "TransferAgreementAutomation/Worker/"]
COPY ["Shared/ProjectOriginClients/ProjectOriginClients.csproj", "Shared/ProjectOriginClients/"]
RUN dotnet restore "TransferAgreementAutomation/Worker/Worker.csproj"
COPY . .
WORKDIR "/src/TransferAgreementAutomation/Worker"
RUN dotnet build "Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TransferAgreementAutomation.Worker.dll"]
