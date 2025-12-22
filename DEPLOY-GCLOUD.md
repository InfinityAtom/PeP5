# PeP Google Cloud Deployment Guide

## Prerequisites

1. **Google Cloud Account** with billing enabled
2. **gcloud CLI** installed: https://cloud.google.com/sdk/docs/install
3. **Docker Desktop** (optional, for local testing)

## Estimated Costs (Monthly)

| Service | Specification | Est. Cost |
|---------|--------------|-----------|
| Cloud SQL for SQL Server | db-custom-1-3840 (1 vCPU, 3.75GB RAM) | ~$50-80 |
| Cloud Run | 1 vCPU, 512MB RAM, auto-scaling | ~$5-20 |
| Cloud Storage | For ExamApp installer hosting | ~$1 |
| **Total** | | **~$56-100/month** |

*Costs vary based on usage. Cloud Run charges only when requests are served.*

---

## Step-by-Step Deployment

### 1. Initial Setup

```powershell
# Login to Google Cloud
gcloud auth login

# Create a new project (or use existing)
gcloud projects create pep-platform --name="PeP Platform"

# Set the project
gcloud config set project pep-platform

# Enable billing (required - do this in Cloud Console)
# https://console.cloud.google.com/billing
```

### 2. Create Cloud SQL for SQL Server

```powershell
# Create SQL Server instance (this takes 5-10 minutes)
gcloud sql instances create pepsqlinstance `
    --database-version=SQLSERVER_2019_STANDARD `
    --tier=db-custom-1-3840 `
    --region=us-central1 `
    --root-password=Songocu_003 `
    --storage-size=10GB `
    --storage-auto-increase

# Create the database
gcloud sql databases create PePDb --instance=pep-sql-instance

# Create a user for the app
gcloud sql users create pepuser `
    --instance=pep-sql-instance `
    --password=YourAppPassword123!
```

### 3. Create Secret for Connection String

```powershell
# Create the connection string secret
$connectionString = "Server=/cloudsql/YOUR_PROJECT:us-central1:pep-sql-instance;Database=PePDb;User Id=pepuser;Password=YourAppPassword123!;TrustServerCertificate=true"

echo $connectionString | gcloud secrets create pep-db-connection --data-file=-

# Grant Cloud Run access to the secret
gcloud secrets add-iam-policy-binding pep-db-connection `
    --member="serviceAccount:YOUR_PROJECT_NUMBER-compute@developer.gserviceaccount.com" `
    --role="roles/secretmanager.secretAccessor"
```

### 4. Build and Deploy

```powershell
# Navigate to the PeP folder
cd c:\Users\fabia\source\repos\PeP

# Build the Docker image with Cloud Build
gcloud builds submit --tag us-central1-docker.pkg.dev/YOUR_PROJECT/pep-docker/pep-webapp

# Deploy to Cloud Run
gcloud run deploy pep-webapp `
    --image us-central1-docker.pkg.dev/YOUR_PROJECT/pep-docker/pep-webapp:latest `
    --region us-central1 `
    --platform managed `
    --allow-unauthenticated `
    --add-cloudsql-instances YOUR_PROJECT:us-central1:pep-sql-instance `
    --set-secrets "ConnectionStrings__DefaultConnection=pep-db-connection:latest" `
    --memory 512Mi `
    --cpu 1 `
    --min-instances 0 `
    --max-instances 10 `
    --port 8080
```

### 5. Configure Custom Domain (Optional)

```powershell
# Map a custom domain
gcloud run domain-mappings create `
    --service pep-webapp `
    --domain pep.yourdomain.com `
    --region us-central1
```

---

## Environment Variables

Set these in Cloud Run:

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string (use Secret Manager) |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ExamAppUpdate__DownloadUrl` | URL to ExamApp installer |

---

## Hosting ExamApp Installer

Upload the installer to Cloud Storage:

```powershell
# Create a bucket
gcloud storage buckets create gs://pep-downloads --location=us-central1

# Make it publicly readable
gcloud storage buckets add-iam-policy-binding gs://pep-downloads `
    --member=allUsers `
    --role=roles/storage.objectViewer

# Upload the installer
gcloud storage cp "PeP.ExamApp\Installer\Output\PeP.ExamApp_Setup_1.0.0.exe" gs://pep-downloads/

# The download URL will be:
# https://storage.googleapis.com/pep-downloads/PeP.ExamApp_Setup_1.0.0.exe
```

---

## Updating the Application

```powershell
# Rebuild and push new image
gcloud builds submit --tag us-central1-docker.pkg.dev/YOUR_PROJECT/pep-docker/pep-webapp

# Cloud Run automatically uses the new image on next request
# Or force a new revision:
gcloud run deploy pep-webapp `
    --image us-central1-docker.pkg.dev/YOUR_PROJECT/pep-docker/pep-webapp:latest `
    --region us-central1
```

---

## Troubleshooting

### View Logs
```powershell
gcloud run services logs read pep-webapp --region us-central1
```

### Check Service Status
```powershell
gcloud run services describe pep-webapp --region us-central1
```

### Connect to Cloud SQL (for debugging)
```powershell
# Install Cloud SQL Proxy
# Then connect locally:
cloud_sql_proxy -instances=YOUR_PROJECT:us-central1:pep-sql-instance=tcp:1433
```

---

## Cost Optimization Tips

1. **Use minimum instances = 0** - No charges when no traffic
2. **Scale down Cloud SQL** - Use `db-f1-micro` for testing (~$10/month)
3. **Use Cloud SQL Express** - Cheaper for small workloads
4. **Set up billing alerts** - Get notified before overspending
