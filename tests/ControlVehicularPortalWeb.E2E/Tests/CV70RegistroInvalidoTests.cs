using System.Text;
using Allure.NUnit;
using Allure.NUnit.Attributes;
using Allure.Net.Commons;
using Microsoft.Playwright;
using NUnit.Framework;

namespace ControlVehicularPortalWeb.E2E.Tests;

[TestFixture]
[Category("CV-70")]
[AllureNUnit]
[NonParallelizable]
public sealed class CV70RegistroInvalidoTests : VehicleE2ETestBase
{
    [Test]
    [AllureFeature("Validación de vehículos")]
    [AllureStory("CV-70 - Registro inválido y corrección")]
    [AllureName("CV-70 - Rechazar año inválido, corregir y archivar")]
    public async Task Registro_AnoFueraDeRangoSeRechaza_YLuegoSeCorrige()
    {
        var config = GetRemoteWriteConfiguration();
        if (config == null)
        {
            Assert.Ignore("CV-70 registro omitido: requiere autorización de escrituras y configuración QA.");
            return;
        }

        var vehicle = CreateUniqueVehicle("CV70R");
        var marker = $"Marca={vehicle.Brand}; Modelo={vehicle.Model}; Placa={vehicle.Plate}; PropietarioId={config.OwnerId}";
        TestContext.Progress.WriteLine($"CV-70 registro run marker: {marker}");
        AllureApi.AddAttachment("CV-70 registro - identificador del vehículo QA", "text/plain", Encoding.UTF8.GetBytes(marker));

        try
        {
            await AuthenticateAsync(config.Email, config.Password, "CV-70 registro");
            await OpenVehicleModuleAsync("CV-70 registro");
            await WaitForVehicleListAsync();

            await Page.Locator(".page-header").GetByRole(
                AriaRole.Button,
                new() { Name = "Registrar vehículo" }).ClickAsync();
            await FillVehicleFieldAsync("Propietario ID *", config.OwnerId.ToString());
            await FillVehicleFieldAsync("Marca *", vehicle.Brand);
            await FillVehicleFieldAsync("Modelo *", vehicle.Model);
            await FillVehicleFieldAsync("Placa *", vehicle.Plate);
            await FillVehicleFieldAsync("Color", vehicle.Color);
            await FillVehicleFieldAsync("Kilometraje actual *", vehicle.Mileage.ToString());

            var yearField = await VehicleFieldAsync("Año *");
            await yearField.FillAsync("1800");
            await AssertYearRangeValidationAsync(yearField, "CV-70 registro");

            var form = Page.Locator(".vehicle-form-card");
            await form.GetByRole(AriaRole.Button, new() { Name = "Registrar vehículo" }).ClickAsync();
            await AssertYearRangeMessageAsync(yearField);
            Assert.That(
                await Page.GetByText("Vehículo registrado correctamente.", new() { Exact = true }).CountAsync(),
                Is.Zero,
                "El formulario inválido no debe mostrar éxito ni ejecutar el alta.");
            Assert.That(await VehicleRow(vehicle.Plate).CountAsync(), Is.Zero,
                "La placa única no debe aparecer en la lista después del rechazo de validación UI.");

            // InputNumber binds through onchange. Tab commits the edit by leaving
            // the field, just as a user would, without replacing the EditContext.
            await yearField.FillAsync(vehicle.Year.ToString());
            await yearField.PressAsync("Tab");
            await Microsoft.Playwright.Assertions.Expect(yearField).ToHaveClassAsync(
                new System.Text.RegularExpressions.Regex(@"\bvalid\b"),
                new() { Timeout = E2EConfiguration.TimeoutMilliseconds });
            await yearField.Locator("xpath=..").Locator(".validation-message")
                .WaitForAsync(new() { State = WaitForSelectorState.Hidden });
            Assert.That(await yearField.EvaluateAsync<bool>("element => element.validity.valid"),
                Is.True, "El año corregido debe cumplir también la validación HTML.");
            await CaptureStageAsync("anio-corregido-mismo-formulario", "CV-70 registro");

            await form.GetByRole(AriaRole.Button, new() { Name = "Registrar vehículo" }).ClickAsync();
            await Page.GetByText("Vehículo registrado correctamente.", new() { Exact = true })
                .WaitForAsync(new() { State = WaitForSelectorState.Visible });

            var row = VehicleRow(vehicle.Plate);
            await row.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            Assert.That(await row.InnerTextAsync(), Does.Contain(vehicle.Brand));
            Assert.That(await row.InnerTextAsync(), Does.Contain(vehicle.Model));
            Assert.That(await row.InnerTextAsync(), Does.Contain(vehicle.Plate));
            await CaptureStageAsync("registro-corregido", "CV-70 registro");

            await OpenVehicleDetailsAsync(vehicle.Plate);
            Assert.That(await ReadVehicleDetailFieldAsync("Año"), Is.EqualTo(vehicle.Year.ToString()));
            Assert.That(await ReadVehicleDetailFieldAsync("Placa"), Is.EqualTo(vehicle.Plate));
            await CaptureStageAsync("consulta-registro-corregido", "CV-70 registro");
            await Page.Locator(".details-card")
                .GetByRole(AriaRole.Button, new() { Name = "Cerrar" }).ClickAsync();

            await ArchiveVehicleAsync(vehicle.Plate, "CV-70 registro");
            TestContext.Progress.WriteLine($"CV-70 registro vehicleId=#{await ReadVehicleIdFromRowAsync(VehicleRow(vehicle.Plate))}; finalState=ARCHIVADO; plate={vehicle.Plate}");
        }
        catch
        {
            TestContext.Progress.WriteLine($"CV-70 registro interrumpido; localizar solo el registro propio: {marker}");
            throw;
        }
    }
}