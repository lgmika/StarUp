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
RUN dotnet tool restore \
    && dotnet ef migrations bundle \
        --project src/Infrastructure/StartupConnect.Infrastructure.csproj \
        --startup-project src/Api/StartupConnect.Api.csproj \
        --configuration Release \
        --no-build \
        --output /app/publish/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "StartupConnect.Api.dll"]
