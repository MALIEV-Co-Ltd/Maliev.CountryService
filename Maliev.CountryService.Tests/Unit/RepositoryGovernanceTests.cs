namespace Maliev.CountryService.Tests.Unit;

/// <summary>
/// Protects the validation-only repository and dependency boundaries.
/// </summary>
public sealed class RepositoryGovernanceTests
{
    private static readonly string[] WorkflowNames =
    [
        "_validate.yml",
        "ci-develop.yml",
        "ci-main.yml",
        "ci-staging.yml",
        "pr-validation.yml"
    ];

    /// <summary>
    /// Validation workflows must not authenticate, publish, deploy, or mutate GitOps.
    /// </summary>
    [Fact]
    public void Workflows_AreValidationOnlyAndCredentialFree()
    {
        foreach (var workflowName in WorkflowNames)
        {
            var source = ReadRepositoryFile(".github", "workflows", workflowName);

            Assert.DoesNotContain("google-github-actions/auth", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("gcloud", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("docker push", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("push: true", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("maliev-gitops", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("GCP_SA_KEY", source, StringComparison.Ordinal);
            Assert.DoesNotContain("GITOPS_PAT", source, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// The shared validation contract must retain all required quality and security gates.
    /// </summary>
    [Fact]
    public void ReusableValidation_ContainsRequiredGatesAndPinnedActions()
    {
        var source = ReadRepositoryFile(".github", "workflows", "_validate.yml");

        Assert.Contains("--locked-mode", source, StringComparison.Ordinal);
        Assert.Contains("--vulnerable --include-transitive", source, StringComparison.Ordinal);
        Assert.Contains("dotnet format", source, StringComparison.Ordinal);
        Assert.Contains("dotnet build", source, StringComparison.Ordinal);
        Assert.Contains("dotnet test", source, StringComparison.Ordinal);
        Assert.Contains("gitleaks_8.30.1_linux_x64.tar.gz", source, StringComparison.Ordinal);
        Assert.Contains("551f6fc83ea457d62a0d98237cbad105af8d557003051f41f3e7ca7b3f2470eb", source, StringComparison.Ordinal);
        Assert.Contains("./gitleaks dir CountryService --no-banner --redact", source, StringComparison.Ordinal);
        Assert.DoesNotContain("gitleaks/gitleaks-action", source, StringComparison.Ordinal);
        Assert.Contains("aquasecurity/trivy-action@ed142fd0673e97e23eac54620cfb913e5ce36c25", source, StringComparison.Ordinal);
        Assert.DoesNotContain("uses: actions/checkout@v", source, StringComparison.Ordinal);
        Assert.DoesNotContain("uses: actions/setup-dotnet@v", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// Restore must depend only on the public NuGet feed.
    /// </summary>
    [Fact]
    public void NuGetConfiguration_UsesOnlyPublicFeed()
    {
        var source = ReadRepositoryFile("nuget.config");

        Assert.Contains("https://api.nuget.org/v3/index.json", source, StringComparison.Ordinal);
        Assert.DoesNotContain("nuget.pkg.github.com", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("packageSourceCredentials", source, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Host build outputs must never overwrite the Linux restore graph in Docker.
    /// </summary>
    [Fact]
    public void DockerIgnore_ExcludesHostBuildArtifacts()
    {
        var source = ReadRepositoryFile(".dockerignore");

        Assert.Contains("**/bin/", source, StringComparison.Ordinal);
        Assert.Contains("**/obj/", source, StringComparison.Ordinal);
        Assert.Contains(".git/", source, StringComparison.Ordinal);
    }

    private static string ReadRepositoryFile(params string[] segments)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            Path.Combine(segments)));

        Assert.True(File.Exists(path), $"Could not find source file: {path}");
        return File.ReadAllText(path);
    }
}
