FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY NcaaBracket.Api/NcaaBracket.Api.csproj NcaaBracket.Api/
RUN dotnet restore NcaaBracket.Api/NcaaBracket.Api.csproj
COPY . .
WORKDIR /src/NcaaBracket.Api
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "NcaaBracket.Api.dll"]
