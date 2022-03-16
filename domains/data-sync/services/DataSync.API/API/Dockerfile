FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR "/src/API"
COPY ./API/API.csproj .
RUN dotnet restore API.csproj
COPY ./API .
RUN dotnet publish "API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443
EXPOSE 5000
COPY --from=build /app/publish .
HEALTHCHECK CMD curl --fail http://localhost:5000/health || exit 1    
ENTRYPOINT ["dotnet", "API.dll"]