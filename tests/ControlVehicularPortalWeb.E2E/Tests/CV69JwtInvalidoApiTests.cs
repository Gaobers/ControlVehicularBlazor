using System.Text;
using Allure.NUnit;
using Allure.NUnit.Attributes;
using Microsoft.Playwright;
using NUnit.Framework;

namespace ControlVehicularPortalWeb.E2E.Tests;

[TestFixture]
[Category("CV-69")]
[AllureNUnit]
[NonParallelizable]
public sealed class CV69JwtInvalidoApiTests : VehicleE2ETestBase
{
    [Test]
    [AllureFeature("Seguridad de API")]
    [AllureStory("CV-69 - Rechazo de token JWT inválido")]
    [AllureName("CV-69 API - La lista protegida rechaza un JWT inválido")]
    public async Task ListadoVehiculos_RechazaJwtInvalido()
    {
        CaptureArtifacts = false;
        await DiscardCurrentTraceAsync();

        var api = await Playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            BaseURL = E2EConfiguration.ApiBaseUrl,
            IgnoreHTTPSErrors = true,
            Timeout = E2EConfiguration.TimeoutMilliseconds
        });

        try
        {
            var response = await api.GetAsync("api/vehiculos/lista", new APIRequestContextOptions
            {
                Headers = new Dictionary<string, string>
                {
                    ["Authorization"] = $"Bearer {CreateJwtWithInvalidSignature()}"
                }
            });

            Assert.That(response.Status, Is.AnyOf(401, 403),
                "La ruta GET protegida debe rechazar el bearer token inválido con Unauthorized o Forbidden.");
            Assert.That(response.Status, Is.Not.EqualTo(200),
                "Una consulta sin credenciales válidas nunca debe devolver datos de vehículos.");
            TestContext.Progress.WriteLine($"CV-69 invalid JWT: GET /api/vehiculos/lista returned HTTP {response.Status}.");
        }
        finally
        {
            await api.DisposeAsync();
        }
    }

    private static string CreateJwtWithInvalidSignature()
    {
        var header = Base64UrlEncode("{\"alg\":\"HS256\",\"typ\":\"JWT\"}");
        var payload = Base64UrlEncode("{\"sub\":\"e2e-invalid-token\",\"exp\":4102444800}");
        var signature = Base64UrlEncode("not-a-valid-signature");
        return $"{header}.{payload}.{signature}";
    }

    private static string Base64UrlEncode(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}