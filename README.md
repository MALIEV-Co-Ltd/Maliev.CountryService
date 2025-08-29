# Maliev.CountryService

This project is the `Maliev.CountryService`, an ASP.NET Core API designed to manage country-related data. It has been migrated to .NET 10 and follows best practices for API development, including the use of Data Transfer Objects (DTOs), a service layer, and secure configuration management.

## Technologies Used

*   ASP.NET Core (.NET 10)
*   Entity Framework Core
*   JWT for authentication
*   Swashbuckle.AspNetCore for API documentation (Swagger)
*   Google Secret Manager (for production secrets via Secrets Store CSI Driver)

## Project Structure

The solution is divided into the following projects:

*   `Maliev.CountryService.Api`: The main API project, containing controllers, DTOs, services, and application startup configuration.
*   `Maliev.CountryService.Data`: The data access layer, containing the `Country` entity and `CountryContext`.
*   `Maliev.CountryService.Tests`: A project for unit and integration tests.

## Building and Running

### Prerequisites

*   .NET 10 SDK
*   Docker (optional, for containerized development/deployment)
*   Visual Studio (recommended for local development)

### Build

To build the project, navigate to the solution root directory and run:

```bash
dotnet build
```

### Running Locally (Development)

1.  **User Secrets**: For local development, manage your sensitive information (like connection strings and JWT keys) using Visual Studio's User Secrets.
    *   Right-click on the `Maliev.CountryService.Api` project in Solution Explorer.
    *   Select "Manage User Secrets".
    *   Add your secrets in `secrets.json` (e.g., `ConnectionStrings:CountryDbContext`, `JwtSecurityKey`, `Jwt:Issuer`, `Jwt:Audience`).

2.  **Run the API**:
    *   Open the solution in Visual Studio and run the `Maliev.CountryService.Api` project.
    *   Alternatively, navigate to the `Maliev.CountryService.Api` directory in your terminal and run:
        ```bash
dotnet run
```
    The API will typically be available at `http://localhost:5185` (HTTP) and `https://localhost:7187` (HTTPS). The Swagger UI will be accessible at `/countries/swagger`.

### Testing

To run the tests, navigate to the `Maliev.CountryService.Tests` directory and run:

```bash
dotnet test
```

## Deployment (Kubernetes with Google Cloud Secret Manager)

This application is designed for deployment to Kubernetes, leveraging Google Cloud Secret Manager for secure secret management via the Secrets Store CSI Driver.

### 1. Prepare Secrets in Google Secret Manager

Ensure the following secrets are created in your Google Cloud Secret Manager:

*   `JwtSecurityKey`
*   `ConnectionStrings-CountryDbContext`
*   `Jwt-Issuer`
*   `Jwt-Audience`

You can create/update secrets using the `gcloud` CLI. For example, to add the `ConnectionStrings-CountryDbContext`:

```bash
# First, create the secret (only needs to be done once per secret name)
gcloud secrets create ConnectionStrings-CountryDbContext \
    --project=your-google-cloud-project-id \
    --replication-policy="automatic" \
    --labels="app=countryservice,env=production"

# Then, add the secret value (can be run multiple times to add new versions)
echo 'your-db-connection-string-value' | gcloud secrets versions add ConnectionStrings-CountryDbContext \
    --project=your-google-cloud-project-id \
    --data-file=-
```
*(Replace placeholders with your actual values and project ID)*

### 2. Apply `SecretProviderClass`

Ensure a `SecretProviderClass` (e.g., `maliev-shared-secrets`) is defined in your Kubernetes cluster to fetch these secrets. An example `SecretProviderClass` YAML is provided in `maliev-shared-secrets.yaml` (you may need to create this file based on the migration task document).

```bash
kubectl apply -f maliev-shared-secrets.yaml
```

### 3. Build and Deploy

Use the `deploy.ps1` PowerShell script located in the project root to build the Docker image and deploy the application to your Kubernetes cluster.

```powershell
.\deploy.ps1
```

This script handles Docker image building, tagging, pushing to Google Artifact Registry, and applying the `deployment.yaml` manifest. The `deploy-service.ps1` script can be used to apply the `service.yaml` manifest.

```powershell
.\deploy-service.ps1
```