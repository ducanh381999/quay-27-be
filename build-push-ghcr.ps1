# Build and push backend image to GHCR.
# Login yourself first: docker login ghcr.io
# Usage:
#   .\build-push-ghcr.ps1
# If MCR pull fails, run first:
#   docker pull mcr.microsoft.com/dotnet/sdk:8.0
#   docker pull mcr.microsoft.com/dotnet/aspnet:8.0

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

$Image = "ghcr.io/ducanh381999/quay-27-be:latest"

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker is not on PATH. Start Docker Desktop first."
}

Write-Host "Building $Image ..."
docker build -t $Image .
if ($LASTEXITCODE -ne 0) { throw "docker build failed. If the error is mcr.microsoft.com, pull the SDK/aspnet images first." }

Write-Host "Pushing $Image ..."
docker push $Image
if ($LASTEXITCODE -ne 0) { throw "docker push failed. Run 'docker login ghcr.io' first." }

Write-Host "Done: $Image"
