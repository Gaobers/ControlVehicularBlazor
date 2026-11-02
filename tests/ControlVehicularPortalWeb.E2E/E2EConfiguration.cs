using NUnit.Framework;

namespace ControlVehicularPortalWeb.E2E;

internal static class E2EConfiguration
{
    public static string BaseUrl =>
        (Environment.GetEnvironmentVariable("E2E_BASE_URL")
            ?? "https://localhost:7293").TrimEnd('/');

    public static string ApiBaseUrl =>
        (Environment.GetEnvironmentVariable("E2E_API_BASE_URL")
            ?? "https://controlvehicular-6s7g.onrender.com/").TrimEnd('/') + "/";

    public static int TimeoutMilliseconds =>
        int.TryParse(Environment.GetEnvironmentVariable("E2E_TIMEOUT_MS"), out var timeout)
            && timeout > 0
                ? timeout
                : 15_000;

    public static bool AllowRemoteWrites =>
        string.Equals(
            Environment.GetEnvironmentVariable("E2E_ALLOW_REMOTE_WRITES"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    public static string ArtifactsDirectory => Path.GetFullPath(
        Environment.GetEnvironmentVariable("E2E_ARTIFACTS_DIR")
            ?? GetRepositoryArtifactsDirectory());

    private static string GetRepositoryArtifactsDirectory()
    {
        for (DirectoryInfo? directory = new(Directory.GetCurrentDirectory());
             directory != null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ControlVehicularPortalWeb.slnx")))
            {
                return Path.Combine(
                    directory.FullName,
                    "tests",
                    "ControlVehicularPortalWeb.E2E",
                    "artifacts");
            }
        }

        return Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "artifacts");
    }
}