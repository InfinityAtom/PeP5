# PeP Google Cloud Deployment Script
# Prerequisites: gcloud CLI installed and authenticated

param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectId,
    
    [string]$Region = "us-central1",
    [string]$ServiceName = "pep-webapp",
    [string]$SqlInstanceName = "pep-sql-instance",
    [string]$DatabaseName = "PePDb"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  PeP Google Cloud Deployment" -ForegroundColor Cyan
Write-Host "  Project: $ProjectId" -ForegroundColor Cyan
Write-Host "  Region: $Region" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Set the project
Write-Host ">> Setting Google Cloud project..." -ForegroundColor Yellow
gcloud config set project $ProjectId

# Enable required APIs
Write-Host ">> Enabling required APIs..." -ForegroundColor Yellow
$apis = @(
    "sqladmin.googleapis.com",
    "run.googleapis.com",
    "cloudbuild.googleapis.com",
    "secretmanager.googleapis.com",
    "artifactregistry.googleapis.com"
)

foreach ($api in $apis) {
    Write-Host "   Enabling $api"
    gcloud services enable $api --quiet
}

# Create Artifact Registry repository (if not exists)
Write-Host ">> Creating Artifact Registry repository..." -ForegroundColor Yellow
$repoExists = gcloud artifacts repositories list --location=$Region --filter="name:pep-docker" --format="value(name)" 2>$null
if (-not $repoExists) {
    gcloud artifacts repositories create pep-docker `
        --repository-format=docker `
        --location=$Region `
        --description="PeP Docker images"
}

# Build and push Docker image
Write-Host ">> Building and pushing Docker image..." -ForegroundColor Yellow
$imageUrl = "$Region-docker.pkg.dev/$ProjectId/pep-docker/$ServiceName"

# Configure Docker for Artifact Registry
gcloud auth configure-docker "$Region-docker.pkg.dev" --quiet

# Build with Cloud Build
gcloud builds submit --tag "$imageUrl`:latest" .

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Docker Image Built Successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "NEXT STEPS:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. CREATE CLOUD SQL INSTANCE (if not exists):" -ForegroundColor Cyan
Write-Host "   Run: .\deploy-gcloud.ps1 -SetupDatabase -ProjectId $ProjectId" -ForegroundColor White
Write-Host ""
Write-Host "2. DEPLOY TO CLOUD RUN:" -ForegroundColor Cyan
Write-Host "   Run: .\deploy-gcloud.ps1 -Deploy -ProjectId $ProjectId" -ForegroundColor White
Write-Host ""
