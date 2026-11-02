using System.Globalization;
using System.Text;
using System.Text.Json;
using Allure.NUnit;
using Allure.NUnit.Attributes;
using Allure.Net.Commons;
using Microsoft.Playwright;
using NUnit.Framework;

namespace ControlVehicularPortalWeb.E2E.Tests;

[TestFixture]
[Category("CV-61")]
[AllureNUnit]
[NonParallelizable]
public sealed class CV61ConsistenciaConsultasTests : VehicleE2ETestBase
{
    [Test]
    [AllureFeature("Consistencia de datos")]
    [AllureStory("CV-61 - Consistencia entre detalle Blazor y consulta API")]
    [AllureName("CV-61 - Comparar detalle UI con GET individual de API")]
    public async Task DetalleBlazor_CoincideConConsultaIndividualApi_YFinalizaArchivado()
    {
        var config = GetRemoteWriteConfiguration();
        if (config == null)
        {
            Assert.Ignore(
                "CV-61 omitido: requiere E2E_ALLOW_REMOTE_WRITES=true y credenciales/propietario QA.");
            return;
        }

        var vehicle = CreateUniqueVehicle("CV61");
        var marker = $"Marca={vehicle.Brand}; Modelo={vehicle.Model}; Placa={vehicle.Plate}; PropietarioId={config.OwnerId}";
        TestContext.Progress.WriteLine($"CV-61 run marker: {marker}");
        AllureApi.AddAttachment("CV-61 identificador del vehículo QA", "text/plain", Encoding.UTF8.GetBytes(marker));

        long? createdVehicleId = null;
        try
        {
            await AuthenticateAsync(config.Email, config.Password, "CV-61");
            await OpenVehicleModuleAsync("CV-61");
            await WaitForVehicleListAsync();
            await RegisterVehicleAsync(config, vehicle, "CV-61");

            var row = VehicleRow(vehicle.Plate);
            createdVehicleId = await ReadVehicleIdFromRowAsync(row);
            await OpenVehicleDetailsAsync(vehicle.Plate);

            var uiOwnerId = long.Parse(await ReadVehicleDetailFieldAsync("Propietario ID"), CultureInfo.InvariantCulture);
            var uiIdText = await Page.Locator(".details-header p").InnerTextAsync();
            Assert.That(uiIdText, Does.StartWith("Registro #"));
            Assert.That(long.TryParse(uiIdText.Replace("Registro #", string.Empty), out var uiVehicleId), Is.True);
            Assert.That(uiVehicleId, Is.EqualTo(createdVehicleId.Value));
            var uiYear = short.Parse(await ReadVehicleDetailFieldAsync("Año"), CultureInfo.InvariantCulture);
            var uiMileageText = await ReadVehicleDetailFieldAsync("Kilometraje");
            var uiMileage = new string(uiMileageText.Where(char.IsDigit).ToArray());
            var uiBrand = await ReadVehicleDetailFieldAsync("Marca");
            var uiModel = await ReadVehicleDetailFieldAsync("Modelo");
            var uiPlate = await ReadVehicleDetailFieldAsync("Placa");
            var uiColor = await ReadVehicleDetailFieldAsync("Color");

            Assert.That(uiBrand, Is.EqualTo(vehicle.Brand));
            Assert.That(uiModel, Is.EqualTo(vehicle.Model));
            Assert.That(uiPlate, Is.EqualTo(vehicle.Plate));
            Assert.That(uiColor, Is.EqualTo(vehicle.Color));
            Assert.That(uiOwnerId, Is.EqualTo(config.OwnerId));
            Assert.That(uiYear, Is.EqualTo(vehicle.Year));
            Assert.That(uiMileage, Is.EqualTo(vehicle.Mileage.ToString("0", CultureInfo.InvariantCulture)));
            Assert.That(await ReadVehicleDetailFieldAsync("Estado"), Is.EqualTo("ACTIVO"));

            await CaptureStageAsync("consulta-blazor", "CV-61");

            var (api, token) = await CreateAuthenticatedApiContextAsync(config.Email, config.Password);
            try
            {
                var response = await api.GetAsync(
                    $"api/vehiculos/{createdVehicleId.Value}",
                    new APIRequestContextOptions
                    {
                        Headers = new Dictionary<string, string>
                        {
                            ["Authorization"] = $"Bearer {token}"
                        }
                    });

                Assert.That(response.Status, Is.EqualTo(200),
                    "La consulta individual protegida de la API debe devolver HTTP 200.");

                using var document = JsonDocument.Parse(await response.TextAsync());
                var apiVehicle = document.RootElement;
                Assert.That(apiVehicle.GetProperty("id").GetInt64(), Is.EqualTo(uiVehicleId));
                Assert.That(apiVehicle.GetProperty("propietarioId").GetInt64(), Is.EqualTo(uiOwnerId));
                Assert.That(apiVehicle.GetProperty("marca").GetString(), Is.EqualTo(uiBrand));
                Assert.That(apiVehicle.GetProperty("modelo").GetString(), Is.EqualTo(uiModel));
                Assert.That(apiVehicle.GetProperty("anio").GetInt32(), Is.EqualTo(uiYear));
                Assert.That(apiVehicle.GetProperty("placa").GetString(), Is.EqualTo(uiPlate));
                Assert.That(apiVehicle.GetProperty("color").GetString(), Is.EqualTo(uiColor));
                Assert.That(apiVehicle.GetProperty("kilometrajeActual").GetDecimal(), Is.EqualTo(vehicle.Mileage));
                Assert.That(apiVehicle.GetProperty("estado").GetString(), Is.EqualTo("ACTIVO"));
            }
            finally
            {
                await api.DisposeAsync();
            }

            await Page.Locator(".details-card")
                .GetByRole(AriaRole.Button, new() { Name = "Cerrar" }).ClickAsync();
            await ArchiveVehicleAsync(vehicle.Plate, "CV-61");

            TestContext.Progress.WriteLine(
                $"CV-61 vehicleId={createdVehicleId.Value}; finalState=ARCHIVADO; plate={vehicle.Plate}");
        }
        catch
        {
            TestContext.Progress.WriteLine(
                $"CV-61 interrumpido. Registro propio a localizar: {marker}; vehicleId={createdVehicleId?.ToString() ?? "pendiente"}.");
            throw;
        }
    }

}