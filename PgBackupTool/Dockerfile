FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER root
RUN apt-get update && apt-get install -y curl gnupg lsb-release && \
    # Download and dearmor the PostgreSQL repository key
    curl -fsSL https://www.postgresql.org/media/keys/ACCC4CF8.asc | gpg --dearmor -o /usr/share/keyrings/postgresql-archive-keyring.gpg && \
    # Add the PostgreSQL APT repository with the signed-by option
    echo "deb [signed-by=/usr/share/keyrings/postgresql-archive-keyring.gpg] http://apt.postgresql.org/pub/repos/apt/ $(lsb_release -cs)-pgdg main" > /etc/apt/sources.list.d/pgdg.list && \
    apt-get update && apt-get install -y postgresql-client-16 && \
    rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PgBackupTool/PgBackupTool.csproj", "PgBackupTool/"]
RUN dotnet restore "./PgBackupTool/PgBackupTool.csproj"
COPY . .
WORKDIR "/src/PgBackupTool"
RUN dotnet build "./PgBackupTool.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PgBackupTool.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PgBackupTool.dll"]
