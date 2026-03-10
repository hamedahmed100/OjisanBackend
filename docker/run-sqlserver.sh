#!/bin/bash
set -e

# Create network
docker network create OjisanNetx 2>/dev/null || true

# Create volumes
docker volume create sqlserver-data 2>/dev/null || true

# Run SQL Server
docker run -d \
  --name sqlserver-student \
  --network OjisanNetx \
  -e ACCEPT_EULA=Y \
  -e MSSQL_SA_PASSWORD='0TgVFbG5FT91561T6Tu9KV9Ac' \
  -p 1433:1433 \
  -v sqlserver-data:/var/opt/mssql \
  --restart unless-stopped \
  mcr.microsoft.com/mssql/server:2022-latest

# Wait and create database
echo "Waiting for SQL Server to start..."
sleep 30
docker exec sqlserver-student /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P '0TgVFbG5FT91561T6Tu9KV9Ac' -C \
  -Q "CREATE DATABASE OjiSanDbPlatform_"

# Create DEV user with permissions
docker exec sqlserver-student /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P '0TgVFbG5FT91561T6Tu9KV9Ac' -C \
  -Q "USE OjiSanDbPlatform_; CREATE LOGIN DEV WITH PASSWORD = '0TgVFbG5FT91561T6Tu9KV9Ac'; CREATE USER DEV FOR LOGIN DEV; ALTER ROLE db_owner ADD MEMBER DEV;"

echo "Done! Database and DEV user created."
echo "Connection: Server=sqlserver-student,1433;Database=OjiSanDbPlatform_;User Id=DEV;Password=0TgVFbG5FT91561T6Tu9KV9Ac;TrustServerCertificate=True;"
