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
public sealed class CV70ActualizacionInvalidaTests : VehicleE2ETestBase
{
    [Test]
    [AllureFeature("Validación de vehículos")]
    [AllureStory("CV-70 - Actualización inválida y conservación de datos")]
    [AllureName("CV-70 - Conservar datos ante año inválido, actualizar y archivar")]
    public async Task Actualizacion_AnoFueraDeRangoConservaDatos_YLuegoActualiza()
    {
        var config = GetRemoteWriteConfiguration();
        if (config == null)
        {
            Assert.Ignore("CV-70 actualización omitida: requiere autorización de escrituras y configuración QA.");
            return;
        }

        var vehicle = CreateUniqueVehicle("CV70U");
        var updatedModel = $"{vehicle.Model}Actualizado";
        var marker = $"Marca={vehicle.Brand}; Modelo={vehicle.Model}; Placa={vehicle.Plate}; PropietarioId={config.OwnerId}";
        TestContext.Progress.WriteLine($"CV-70 actualización run marker: {marker}");
        AllureApi.AddAttachment("CV-70 actualización - identificador del vehículo QA", "text/plain", Encoding.UTF8.GetBytes(marker));

        try
        {
            await AuthenticateAsync(config.Email, config.Password, "CV-70 actualización");
            await OpenVehicleModuleAsync("CV-70 actualización");
            await WaitForVehicleListAsync();
            await RegisterVehicleAsync(config, vehicle, "CV-70 actualización");

            var row = VehicleRow(vehicle.Plate);
            var vehicleId = await ReadVehicleIdFromRowAsync(row);
            await OpenVehicleDetailsAsync(vehicle.Plate);
            var originalModel = await ReadVehicleDetailFieldAsync("Modelo");
            var originalYear = await ReadVehicleDetailFieldAsync("Año");
            Assert.That(originalModel, Is.EqualTo(vehicle.Model));
            Assert.That(originalYear, Is.EqualTo(vehicle.Year.ToString()));
            await Page.Locator(".details-card")
                .GetByRole(AriaRole.Button, new() { Name = "Cerrar" }).ClickAsync();

            await VehicleRow(vehicle.Plate).GetByRole(AriaRole.Button, new() { Name = "Editar" }).ClickAsync();
            var yearField = await VehicleFieldAsync("Año *");
            await yearField.FillAsync("1800");
            await AssertYearRangeValidationAsync(yearField, "CV-70 actualización");

            var form = Page.Locator(".vehicle-form-card");
            await form.GetByRole(AriaRole.Button, new() { Name = "Guardar cambios" }).ClickAsync();
            await AssertYearRangeMessageAsync(yearField);
            Assert.That(
                await Page.GetByText("Vehículo actualizado correctamente.", new() { Exact = true }).CountAsync(),
                Is.Zero,
                "La actualización inválida no debe mostrar éxito.");

            await form.GetByRole(AriaRole.Button, new() { Name = "Cancelar" }).ClickAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = "Actualizar" }).ClickAsync();
            await VehicleRow(vehicle.Plate).WaitForAsync(new() { State = WaitForSelectorState.Visible });
            Assert.That(await VehicleRow(vehicle.Plate).InnerTextAsync(), Does.Contain(originalModel));

            await OpenVehicleDetailsAsync(vehicle.Plate);
            Assert.That(await ReadVehicleDetailFieldAsync("Modelo"), Is.EqualTo(originalModel));
            Assert.That(await ReadVehicleDetailFieldAsync("Año"), Is.EqualTo(originalYear));
            await CaptureStageAsync("datos-originales-conservados", "CV-70 actualización");
            await Page.Locator(".details-card")
                .GetByRole(AriaRole.Button, new() { Name = "Cerrar" }).ClickAsync();

            await VehicleRow(vehicle.Plate).GetByRole(AriaRole.Button, new() { Name = "Editar" }).ClickAsync();
            await (await VehicleFieldAsync("Modelo *")).FillAsync(updatedModel);
            await Page.Locator(".vehicle-form-card")
                .GetByRole(AriaRole.Button, new() { Name = "Guardar cambios" }).ClickAsync();
            await Page.GetByText("Vehículo actualizado correctamente.", new() { Exact = true })
                .WaitForAsync(new() { State = WaitForSelectorState.Visible });

            await Page.GetByRole(AriaRole.Button, new() { Name = "Actualizar" }).ClickAsync();
            await VehicleRow(vehicle.Plate).WaitForAsync(new() { State = WaitForSelectorState.Visible });
            Assert.That(await VehicleRow(vehicle.Plate).InnerTextAsync(), Does.Contain(updatedModel));
            await OpenVehicleDetailsAsync(vehicle.Plate);
            Assert.That(await ReadVehicleDetailFieldAsync("Modelo"), Is.EqualTo(updatedModel));
            await CaptureStageAsync("actualizacion-valida-persistida", "CV-70 actualización");
            await Page.Locator(".details-card")
                .GetByRole(AriaRole.Button, new() { Name = "Cerrar" }).ClickAsync();

            await ArchiveVehicleAsync(vehicle.Plate, "CV-70 actualización");
            TestContext.Progress.WriteLine(
                $"CV-70 actualización vehicleId=#{vehicleId}; finalState=ARCHIVADO; plate={vehicle.Plate}");
        }
        catch
        {
            TestContext.Progress.WriteLine($"CV-70 actualización interrumpida; localizar solo el registro propio: {marker}");
            throw;
        }
    }
}