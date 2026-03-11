# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy repo-level props (TargetFramework etc.) and project files for restore
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/Web/Web.csproj", "src/Web/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/Web/Web.csproj"

# Copy application source
COPY src/ src/

# Publish application (skip NSwag in container build)
WORKDIR /src/src/Web
RUN dotnet publish "Web.csproj" \
    -p:SkipNSwag=true \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# Copy published output from build stage
COPY --from=build /app/publish .

# API listens on 8080 inside the container.
# On the server, run with -p 5000:8080 so the API is on host port 5000 (Jenkins uses 8080).
# Result: API at http://host:5000, Jenkins at http://host:8080.
EXPOSE 8080

# Configure ASP.NET to listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "OjisanBackend.Web.dll"]
