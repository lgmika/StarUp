FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY StartupConnect.slnx ./
COPY src/Api/StartupConnect.Api.csproj src/Api/
COPY src/Application/StartupConnect.Application.csproj src/Application/
COPY src/Domain/StartupConnect.Domain.csproj src/Domain/
COPY src/Infrastructure/StartupConnect.Infrastructure.csproj src/Infrastructure/
COPY src/Shared/StartupConnect.Shared.csproj src/Shared/

RUN dotnet restore src/Api/StartupConnect.Api.csproj

COPY . .
RUN dotnet publish src/Api/StartupConnect.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "StartupConnect.Api.dll"]
