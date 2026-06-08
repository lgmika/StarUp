FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY StartupConnect.slnx ./
COPY src/StartupConnect.Api/StartupConnect.Api.csproj src/StartupConnect.Api/
COPY src/StartupConnect.Application/StartupConnect.Application.csproj src/StartupConnect.Application/
COPY src/StartupConnect.Domain/StartupConnect.Domain.csproj src/StartupConnect.Domain/
COPY src/StartupConnect.Infrastructure/StartupConnect.Infrastructure.csproj src/StartupConnect.Infrastructure/
COPY src/StartupConnect.Shared/StartupConnect.Shared.csproj src/StartupConnect.Shared/

RUN dotnet restore src/StartupConnect.Api/StartupConnect.Api.csproj

COPY . .
RUN dotnet publish src/StartupConnect.Api/StartupConnect.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "StartupConnect.Api.dll"]

