# Deployment & CI/CD

## Ports on the server

- **Jenkins:** http://144.91.70.227:8080 (port **8080** on the host)
- **API:** http://144.91.70.227:5000 (port **5000** on the host)

The API container listens on port **8080 inside the container**; the pipeline maps it to host port **5000** so it does not conflict with Jenkins on 8080.

## Jenkins

- **Jenkins URL:** http://144.91.70.227:8080  
- Jenkins runs on the Contabo VPS. The pipeline (see `ci/Jenkinsfile`) should be run from that server so that `docker build` and `docker run` execute on the same host where the API will run.

## Pipeline overview

1. **Checkout** – Pulls the latest code from the repository.
2. **Build Docker image** – Builds the API image from `docker/Dockerfile` (context: repo root).
3. **Stop / Remove existing container** – Stops and removes the running `ojisan-api` container if present.
4. **Run new container** – Starts the new container with the required environment variables.

## Required environment variables on Jenkins

Configure these on the Jenkins node (or as pipeline credentials) so the “Run New Container” step can pass them into the container:

| Variable | Description |
|----------|-------------|
| `DB_CONNECTION_STRING` | SQL Server connection string for the API. |
| `S3_ACCESS_KEY` | Contabo Object Storage access key. |
| `S3_SECRET_KEY` | Contabo Object Storage secret key. |
| `S3_BUCKET` | Bucket name (e.g. `photos`). Default in pipeline: `photos`. |
| `S3_ENDPOINT` | Contabo endpoint (e.g. `https://eu2.contabostorage.com`). |
| `S3_PUBLIC_BASE_URL` | Optional. Base URL for public file URLs. |

## Local Docker test

From the repo root:

```bash
# Build
docker build -f docker/Dockerfile -t ojisan-api:local .

# Run (replace with your real connection string and S3 credentials)
# -p 5000:8080 = host port 5000 → container port 8080 (use 8080:8080 locally if Jenkins is not on this machine)
docker run -d --name ojisan-api -p 5000:8080 \
  -e ConnectionStrings__OjisanBackendDb="Server=...;Database=...;..." \
  -e S3_ACCESS_KEY="your-key" \
  -e S3_SECRET_KEY="your-secret" \
  -e S3_BUCKET=photos \
  -e S3_ENDPOINT=https://eu2.contabostorage.com \
  ojisan-api:local
```

- **On the server** (where Jenkins runs): use `-p 5000:8080` so the API is at http://144.91.70.227:5000 and does not conflict with Jenkins on :8080.
- **Local test** (no Jenkins): you can use `-p 8080:8080` and reach the API at http://localhost:8080.
