using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Allure.Net.Commons;
using Microsoft.Playwright;
using NUnit.Framework;

namespace ControlVehicularPortalWeb.E2E;

public abstract class E2ETestBase
{
    private IPlaywright? playwright;
    private IBrowser? browser;
    private IBrowserContext? context;
    private bool tracingStarted;

    protected IPage Page { get; private set; } = null!;
    protected IPlaywright BrowserPlaywright => playwright
        ?? throw new InvalidOperationException("Playwright no está inicializado.");
    protected bool CaptureArtifacts { get; set; } = true;

    [SetUp]
    public async Task CreateUnauthenticatedBrowserContextAsync()
    {
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });
        Page = await context.NewPageAsync();
        Page.SetDefaultTimeout(E2EConfiguration.TimeoutMilliseconds);
        Page.SetDefaultNavigationTimeout(E2EConfiguration.TimeoutMilliseconds);

        await StartTracingAsync();
    }

    [TearDown]
    public async Task SaveEvidenceAndCloseBrowserAsync()
    {
        if (context == null || browser == null || playwright == null)
        {
            return;
        }

        var testName = SanitizeFileName(TestContext.CurrentContext.Test.Name);
        var runId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
        var artifactDirectory = E2EConfiguration.ArtifactsDirectory;
        Directory.CreateDirectory(artifactDirectory);

        var screenshotPath = Path.Combine(artifactDirectory, $"{runId}-{testName}.png");
        var tracePath = Path.Combine(artifactDirectory, $"{runId}-{testName}.zip");
        var rawTracePath = Path.Combine(artifactDirectory, $"{runId}-{testName}.raw.zip");

        try
        {
            if (CaptureArtifacts)
            {
                await Page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = screenshotPath,
                    FullPage = true
                });
            }

            if (tracingStarted)
            {
                await context.Tracing.StopAsync(new TracingStopOptions
                {
                    Path = CaptureArtifacts ? rawTracePath : null
                });
                tracingStarted = false;
            }

            if (CaptureArtifacts && File.Exists(rawTracePath))
            {
                try
                {
                    await CreateRedactedTraceAsync(rawTracePath, tracePath);
                }
                finally
                {
                    File.Delete(rawTracePath);
                }
            }

            if (CaptureArtifacts)
            {
                AllureApi.AddAttachment(
                    "Captura de pantalla final",
                    "image/png",
                    await File.ReadAllBytesAsync(screenshotPath));
                if (File.Exists(tracePath))
                {
                    AllureApi.AddAttachment(
                        "Traza de Playwright (datos sensibles redactados)",
                        "application/zip",
                        await File.ReadAllBytesAsync(tracePath));
                }

                TestContext.Progress.WriteLine($"Screenshot: {screenshotPath}");
                TestContext.Progress.WriteLine($"Playwright trace: {tracePath}");
            }
        }
        catch (Exception exception)
        {
            TestContext.Progress.WriteLine($"No se pudieron guardar todas las evidencias: {exception.Message}");
        }
        finally
        {
            await context.CloseAsync();
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    protected async Task DiscardCurrentTraceAsync()
    {
        if (context == null || !tracingStarted)
        {
            return;
        }

        await context.Tracing.StopAsync(new TracingStopOptions());
        tracingStarted = false;
    }

    protected async Task StartTracingAsync()
    {
        if (context == null || tracingStarted)
        {
            return;
        }

        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
        tracingStarted = true;
    }

    protected async Task CaptureStageAsync(string stageName, string caseId = "CV-60")
    {
        if (!CaptureArtifacts)
        {
            return;
        }

        var safeStageName = SanitizeFileName(stageName);
        var filename = $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{safeStageName}.png";
        var path = Path.Combine(E2EConfiguration.ArtifactsDirectory, filename);
        Directory.CreateDirectory(E2EConfiguration.ArtifactsDirectory);

        var screenshot = await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = path,
            FullPage = true
        });

        AllureApi.AddAttachment($"{caseId} - {stageName}", "image/png", screenshot);
        TestContext.Progress.WriteLine($"{caseId} evidence ({stageName}): {path}");
    }

    private static async Task CreateRedactedTraceAsync(string sourcePath, string destinationPath)
    {
        using var sourceArchive = ZipFile.OpenRead(sourcePath);
        using var destinationStream = File.Create(destinationPath);
        using var destinationArchive = new ZipArchive(
            destinationStream,
            ZipArchiveMode.Create,
            leaveOpen: false);

        foreach (var sourceEntry in sourceArchive.Entries)
        {
            var destinationEntry = destinationArchive.CreateEntry(
                sourceEntry.FullName,
                CompressionLevel.Optimal);
            await using var sourceEntryStream = sourceEntry.Open();
            await using var destinationEntryStream = destinationEntry.Open();

            if (IsTextTraceEntry(sourceEntry.FullName))
            {
                using var reader = new StreamReader(sourceEntryStream, Encoding.UTF8);
                var content = await reader.ReadToEndAsync();
                var redactedContent = RedactTraceJsonLines(content);
                await using var writer = new StreamWriter(
                    destinationEntryStream,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                await writer.WriteAsync(redactedContent);
            }
            else
            {
                await sourceEntryStream.CopyToAsync(destinationEntryStream);
            }
        }
    }

    private static bool IsTextTraceEntry(string entryName)
    {
        var extension = Path.GetExtension(entryName);
        return extension is ".trace" or ".network" or ".json" or ".txt";
    }

    private static string RedactTraceJsonLines(string content)
    {
        var lines = content.Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            if (string.IsNullOrWhiteSpace(lines[index]))
            {
                continue;
            }

            try
            {
                var node = JsonNode.Parse(lines[index]);
                RedactSensitiveValues(node);
                lines[index] = node?.ToJsonString() ?? lines[index];
            }
            catch (JsonException)
            {
                lines[index] = "[REDACTED NON-JSON TRACE ENTRY]";
            }
        }

        return string.Join('\n', lines);
    }

    private static void RedactSensitiveValues(JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            var headerName = jsonObject["name"]?.GetValue<string>();
            if (IsSensitiveName(headerName))
            {
                jsonObject["value"] = "[REDACTED]";
            }

            foreach (var property in jsonObject.ToList())
            {
                if (IsSensitiveName(property.Key))
                {
                    jsonObject[property.Key] = "[REDACTED]";
                }
                else
                {
                    RedactSensitiveValues(property.Value);
                }
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var child in jsonArray)
            {
                RedactSensitiveValues(child);
            }
        }
    }

    private static bool IsSensitiveName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return name.Contains("authorization", StringComparison.OrdinalIgnoreCase)
            || name.Contains("cookie", StringComparison.OrdinalIgnoreCase)
            || name.Contains("password", StringComparison.OrdinalIgnoreCase)
            || name.Contains("passwd", StringComparison.OrdinalIgnoreCase)
            || name.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || name.Contains("token", StringComparison.OrdinalIgnoreCase)
            || name.Contains("clave", StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizeFileName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(value
            .Select(character => invalidCharacters.Contains(character) ? '_' : character)
            .ToArray());

        return sanitized.Replace(' ', '_');
    }
}