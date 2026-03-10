# Test Docker locally

Run these from the **repo root** (OjisanBackend folder).

## 0. (Optional) Start SQL Server in Docker

If you want the API to use SQL Server in Docker (same as your server DB setup):

```bash
# From repo root (Git Bash or WSL on Windows)
bash docker/run-sqlserver.sh
```

This creates network `OjisanNetx`, container `sqlserver-student`, database `OjiSanDbPlatform_`, and user `DEV`.  
Use this **same network** when running the API container so it can reach the DB.

## 1. Build the image

```bash
docker build -f docker/Dockerfile -t ojisan-api:local .
```

## 2. Run the container

Replace S3 values with your real ones. Use the connection string that matches your DB (local SQL Server vs Docker SQL Server).

**With SQL Server in Docker** (after running `docker/run-sqlserver.sh`), join the same network and use the DB container name as server:

**PowerShell (Windows):**
```powershell
docker run -d --name ojisan-api -p 8080:8080 --network OjisanNetx -e "ConnectionStrings__OjisanBackendDb=Server=sqlserver-student,1433;Database=OjiSanDbPlatform_;User Id=DEV;Password=0TgVFbG5FT91561T6Tu9KV9Ac;TrustServerCertificate=True;" -e "S3_ACCESS_KEY=your-contabo-access-key" -e "S3_SECRET_KEY=your-contabo-secret-key" -e "S3_BUCKET=photos" -e "S3_ENDPOINT=https://eu2.contabostorage.com" ojisan-api:local
```

**Bash (Linux/macOS/Git Bash):**
```bash
docker run -d --name ojisan-api -p 8080:8080 --network OjisanNetx \
  -e "ConnectionStrings__OjisanBackendDb=Server=sqlserver-student,1433;Database=OjiSanDbPlatform_;User Id=DEV;Password=0TgVFbG5FT91561T6Tu9KV9Ac;TrustServerCertificate=True;" \
  -e "S3_ACCESS_KEY=your-contabo-access-key" \
  -e "S3_SECRET_KEY=your-contabo-secret-key" \
  -e "S3_BUCKET=photos" \
  -e "S3_ENDPOINT=https://eu2.contabostorage.com" \
  ojisan-api:local
```

**With SQL Server on the host machine** (no DB container), use `host.docker.internal` and your DB name/user:

```powershell
docker run -d --name ojisan-api -p 8080:8080 -e "ConnectionStrings__OjisanBackendDb=Server=host.docker.internal,1433;Database=YourDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;" -e "S3_ACCESS_KEY=..." -e "S3_SECRET_KEY=..." -e "S3_BUCKET=photos" -e "S3_ENDPOINT=https://eu2.contabostorage.com" ojisan-api:local
```

## 3. Check it’s running

```bash
docker ps
curl http://localhost:8080/health
```

## 4. Stop and remove (when done)

```bash
docker stop ojisan-api
docker rm ojisan-api
```

If you started SQL Server with `run-sqlserver.sh`: `docker stop sqlserver-student` (and `docker rm sqlserver-student` if you want to remove it).

---

- **API URL (local):** http://localhost:8080  
- **Swagger/OpenAPI:** http://localhost:8080/api
