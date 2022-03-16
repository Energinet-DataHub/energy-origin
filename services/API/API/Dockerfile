FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR "/src/API"
COPY ./services/API/API .
RUN dotnet publish "API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "API.dll"]