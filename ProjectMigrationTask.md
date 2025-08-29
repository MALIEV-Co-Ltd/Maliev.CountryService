# Project Migration Task

## Service-Specific Configuration

*   **Service Name**: `CountryService`
*   **API Project Name**: `Maliev.CountryService.Api`
*   **Google Cloud Project ID**: `maliev-website`
*   **Swagger Route Prefix**: `countries`
*   **Boilerplate API Project Path**: `Maliev.CountryService.Api`

---

## Objective

Migrate the .NET project located in the `migration_source` folder to adhere to modern .NET 10 standards and best practices. The `reference_project` folder contains a sample project that demonstrates the desired structure, conventions, and best practices for the migrated project. A boilerplate project, named `$(API_PROJECT_NAME)`, is provided as a starting point.

## Task Details

### 1. API Controller Refinement

Ensure the migrated API Controller (specifically for CRUD operations) adheres to the following best practices:

*   **Data Transfer Objects (DTOs)**: Implement DTOs for both API requests (e.g., Create/Update requests with robust input validation using Data Annotations) and responses.
*   **Service Layer**: Introduce a dedicated service layer (or repository pattern) to encapsulate business logic and interact with the database context. The controller must depend on this service layer, not directly on the database context.
*   **Logging**: Inject and utilize `ILogger` for comprehensive logging within the controller and service layer, capturing important events and potential errors.
*   **Asynchronous Operations**: Ensure all API operations are asynchronous using `async/await` for improved performance and responsiveness.
*   **Error Handling**: Implement robust error handling mechanisms, returning appropriate HTTP status codes (e.g., `200 OK`, `201 Created`, `204 No Content`, `400 Bad Request`, `404 Not Found`).

### 2. Deployment Configuration Migration

Migrate all relevant deployment files from the `reference_project` to the project root folder. This includes:

*   **Kubernetes Manifests**: `deployment.yaml`, `service.yaml`.
*   **Deployment Scripts**: Any associated PowerShell scripts (e.g., `deploy.ps1`, `deploy-service.ps1`).
*   **Secrets Management Integration**: 
    *   **External Secrets Store**: Ensure all sensitive data (e.g., `JwtSecurityKey`, database connection strings like `ConnectionStrings-$(SERVICE_NAME)DbContext`) are stored in an external secrets management system (e.g., Google Secret Manager).
        *   **Note**: The LLM cannot directly create secrets in the external store. This is a manual step for the user.
        *   **Instruction for LLM**: Ask the user to run the following `gcloud` commands manually to create and add the `ConnectionStrings-$(SERVICE_NAME)DbContext` secret to Google Secret Manager. Remind them to replace the placeholder value with their actual connection string and their Google Cloud Project ID.
        ```bash
        # First, create the secret (only needs to be done once per secret name)
        gcloud secrets create ConnectionStrings-$(SERVICE_NAME)DbContext \
            --project=$(GCP_PROJECT_ID) \
            --replication-policy="automatic" \
            --labels="app=$(SERVICE_NAME | ToLower),env=production"

        # Then, add the secret value (can be run multiple times to add new versions)
        echo 'your-db-connection-string-value' | gcloud secrets versions add ConnectionStrings-$(SERVICE_NAME)DbContext \
            --project=$(GCP_PROJECT_ID) \
            --data-file=-
        ```
    *   **`SecretProviderClass` Definition**: Create or update a `SecretProviderClass` Kubernetes resource (e.g., `maliev-shared-secrets.yaml`) that:
        *   References the secrets stored in the external secrets management system.
        *   **Crucially, uses literal project IDs and secret names** (e.g., `projects/$(GCP_PROJECT_ID)/secrets/your-secret-name/versions/latest`), **not shell command substitutions**.
        *   Specifies the `fileName` for each secret, which will be the name of the file mounted into the pod (e.g., `JwtSecurityKey`, `$(SERVICE_NAME)DbContext`).
    *   **Deployment Manifest Update**: Ensure `deployment.yaml` correctly references the `SecretProviderClass` (e.g., `secretProviderClass: "maliev-shared-secrets"`) to enable the Secrets Store CSI Driver to mount the secrets into the application pods.

### 5. Project File (`.csproj`) Cleanup and Warning Resolution

*   **Remove Unused Configurations**: Ensure `<Configurations>` property in `.csproj` files only contains `Debug` and `Release`.
*   **Remove XML Documentation References**: Remove `<DocumentationFile>` properties and any `<None Update="*.xml">` items from `.csproj` files to prevent generation of XML documentation files.
*   **Remove Resource File References**: Remove `EmbeddedResource Update="Properties\Resources.resx"` and `Compile Update="Properties\Resources.Designer.cs"` items from `.csproj` files if `Resources.resx` is not used.
*   **Resolve Nullability Warnings**: Address `CS8618` (Non-nullable property must contain a non-null value) by using the `required` keyword for properties in DTOs. Address `CS8603` (Possible null reference return) by correctly specifying nullable return types in service interfaces/implementations.
*   **Resolve Missing XML Comments**: Add XML documentation comments (`CS1591`) to public types and members in `CountryContext.cs`, `Country.cs`, and test files.
*   **Resolve Missing Using Directives**: Ensure all necessary `using` directives (e.g., `System.Threading.Tasks`, `Xunit`, `System.ComponentModel.DataAnnotations`) are present in relevant files.

*   **Secret Management**: Remove all sensitive information (e.g., connection strings, JWT keys) from `appsettings.json` and `appsettings.Development.json`. These should be managed exclusively via secure environment-specific configurations (like Visual Studio User Secrets for local development, or mounted secrets from CSI driver for production).
*   **Local Development Setup**: Update `launchSettings.json` to correctly configure the local development environment, including setting the `launchUrl` to the Swagger UI page for easy debugging.

### 4. Boilerplate Cleanup

*   Remove all traces of boilerplate code (e.g., 'WeatherForecast' related files/code, unused comments, default project files) from the migrated project to ensure a clean and focused codebase.

## Process Guidance for the LLM

As part of your process, you **must** perform the following:

1.  **Comprehensive To-Do List**: Create a detailed, step-by-step to-do list to guide yourself through the entire migration process. This list should be dynamic and updated as tasks are completed or new challenges arise.
2.  **Generate `GEMINI.md`**: After completing the migration, generate or update the `GEMINI.md` file in the project root. This file should summarize the key changes made, the rationale behind them, and any important considerations for future development or deployment.
3.  **Update `README.md`**: Update the project's `README.md` file to reflect the new project structure, build instructions, running instructions, and any other relevant information for a developer getting started with the migrated project.

## Verification Steps

Before considering the task complete, perform the following verification steps:

1.  **Build the Solution**: Execute `dotnet build` from the solution root to ensure the entire project compiles without errors.
2.  **Run Tests**: Execute `dotnet test` from the test project directory to confirm that all existing tests pass and no regressions have been introduced.
3.  **Cleanup**: The warning from the `dotnet build` command must be addressed after the build and test as completed.
