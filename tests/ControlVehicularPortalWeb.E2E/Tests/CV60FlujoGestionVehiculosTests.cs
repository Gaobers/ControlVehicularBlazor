using System.Security.Cryptography;
using System.Text;
using Allure.NUnit;
using Allure.NUnit.Attributes;
using Allure.Net.Commons;
using Microsoft.Playwright;
using NUnit.Framework;

namespace ControlVehicularPortalWeb.E2E.Tests;

[TestFixture]
[AllureNUnit]
[NonParallelizable]
public sealed class CV60FlujoGestionVehiculosTests : VehicleE2ETestBase
{
    [Test]
    [Category("Preflight")]
    [AllureFeature("Preflight CV-60")]
    [AllureName("CV-60 preflight - Login y consulta de vehículos sin escrituras")]
    public async Task Preflight_LoginYModuloVehiculos_SoloLectura()
    {
        CaptureArtifacts = false;

        var email = Environment.GetEnvironmentVariable("E2E_EMAIL");
        var password = Environment.GetEnvironmentVariable("E2E_PASSWORD");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            Assert.Ignore("Preflight omitido: E2E_EMAIL o E2E_PASSWORD no están configurados.");
            return;
        }

        await AuthenticateAsync(email, password);
        await OpenVehicleModuleAsync();
        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('.vehicles-table tbody tr').length > 0 " +
            "|| document.querySelector('.table-message h3')?.textContent?.trim() === 'No se encontraron vehículos' " +
            "|| Boolean(document.querySelector('.error-message'))",
            null,
            new() { Timeout = E2EConfiguration.TimeoutMilliseconds });

        var rowCount = await Page.Locator(".vehicles-table tbody tr").CountAsync();
        Assert.That(await Page.Locator(".error-message").CountAsync(), Is.Zero,
            "La carga de vehículos no debe terminar en error.");
        Assert.That(rowCount, Is.GreaterThan(0),
            "Se necesita al menos un vehículo visible para consultar su Propietario ID.");

        Assert.That(
            await Page.GetByRole(AriaRole.Heading, new() { Name = "Gestión de Vehículos" }).IsVisibleAsync(),
            Is.True,
            "La sesión debe poder abrir el módulo protegido.");

        var firstRow = Page.Locator(".vehicles-table tbody tr").First;
        await firstRow.GetByRole(AriaRole.Button, new() { Name = "Ver" }).ClickAsync();

        var details = Page.Locator(".details-card");
        await details.GetByRole(AriaRole.Heading, new() { Name = "Detalle del vehículo" })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var ownerIdText = await details.Locator(".details-grid div")
            .Filter(new() { HasTextString = "Propietario ID" })
            .Locator("strong")
            .InnerTextAsync();

        Assert.That(long.TryParse(ownerIdText, out var ownerId), Is.True,
            "El detalle debe exponer un Propietario ID numérico.");
        Assert.That(ownerId, Is.GreaterThan(0),
            "El Propietario ID consultado debe ser positivo.");

        TestContext.Progress.WriteLine($"Read-only vehicle detail Propietario ID: {ownerId}");
        await details.GetByRole(AriaRole.Button, new() { Name = "Cerrar" }).ClickAsync();
    }

    [Test]
    [Category("CV-60")]
    [AllureFeature("Gestión de vehículos")]
    [AllureStory("CV-60 - Flujo E2E de gestión de vehículos")]
    [AllureName("CV-60 - Registrar, consultar, actualizar y archivar vehículo")]
    public async Task FlujoCompleto_DejaElVehiculoArchivado()
    {
        if (!E2EConfiguration.AllowRemoteWrites)
        {
            CaptureArtifacts = false;
            Assert.Ignore(
                "CV-60 omitido: configure E2E_ALLOW_REMOTE_WRITES=true para autorizar escrituras remotas.");
        }

        var email = Environment.GetEnvironmentVariable("E2E_EMAIL");
        var password = Environment.GetEnvironmentVariable("E2E_PASSWORD");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            CaptureArtifacts = false;
            Assert.Ignore(
                "CV-60 omitido: se requieren E2E_EMAIL y E2E_PASSWORD para el usuario de pruebas.");
            return;
        }

        if (!long.TryParse(Environment.GetEnvironmentVariable("E2E_OWNER_ID"), out var ownerId)
            || ownerId < 1)
        {
            CaptureArtifacts = false;
            Assert.Ignore("CV-60 omitido: E2E_OWNER_ID debe ser un identificador positivo válido.");
            return;
        }

        var uniqueToken = $"{DateTime.UtcNow:MMddHHmmssfff}{RandomNumberGenerator.GetInt32(0, 1_000_000):D6}";
        var brand = $"E2E{uniqueToken}";
        var model = $"CV60{uniqueToken}";
        var updatedModel = $"{model}U";
        var plate = $"P{uniqueToken}";

        var recordMarker = $"Marca={brand}; Modelo={model}; Placa={plate}; PropietarioId={ownerId}";
        TestContext.Progress.WriteLine($"CV-60 run marker: {recordMarker}");
        AllureApi.AddAttachment(
            "CV-60 identificador único del registro",
            "text/plain",
            Encoding.UTF8.GetBytes(recordMarker));

        try
        {
            await AuthenticateAsync(email!, password!);
            await OpenVehicleModuleAsync();
            await RegisterVehicleAsync(ownerId, brand, model, plate);
            await ConsultVehicleAsync(brand, model, plate);
            await UpdateVehicleAsync(plate, updatedModel);
            await VerifyUpdatedVehicleAsync(plate, brand, updatedModel);
            await ArchiveVehicleAsync(plate);
            await VerifyArchivedVehicleAsync(plate);
        }
        catch
        {
            TestContext.Progress.WriteLine(
                $"CV-60 se interrumpió. Conserve este identificador para localizar cualquier registro ya creado: {recordMarker}");
            throw;
        }
    }

    private async Task RegisterVehicleAsync(long ownerId, string brand, string model, string plate)
    {
        await Page.Locator(".page-header").GetByRole(
            AriaRole.Button,
            new() { Name = "Registrar vehículo" }).ClickAsync();

        await FillVehicleFieldAsync("Propietario ID *", ownerId.ToString());
        await FillVehicleFieldAsync("Marca *", brand);
        await FillVehicleFieldAsync("Modelo *", model);
        await FillVehicleFieldAsync("Año *", DateTime.UtcNow.Year.ToString());
        await FillVehicleFieldAsync("Placa *", plate);
        await FillVehicleFieldAsync("Kilometraje actual *", "1200");

        var form = Page.Locator(".vehicle-form-card");
        await form.GetByRole(AriaRole.Button, new() { Name = "Registrar vehículo" }).ClickAsync();
        await Page.GetByText("Vehículo registrado correctamente.", new() { Exact = true })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });

        var row = VehicleRow(plate);
        await row.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        Assert.That(await row.InnerTextAsync(), Does.Contain(brand));
        Assert.That(await row.InnerTextAsync(), Does.Contain(model));
        Assert.That(await row.InnerTextAsync(), Does.Contain(plate));

        TestContext.Progress.WriteLine($"CV-60 registered row: {await row.InnerTextAsync()}");
        await CaptureStageAsync("vehiculo-registrado");
    }

    private async Task ConsultVehicleAsync(string brand, string model, string plate)
    {
        var row = VehicleRow(plate);
        await row.GetByRole(AriaRole.Button, new() { Name = "Ver" }).ClickAsync();

        var details = Page.Locator(".details-card");
        await details.GetByRole(AriaRole.Heading, new() { Name = "Detalle del vehículo" })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });

        var detailsText = await details.InnerTextAsync();
        Assert.That(detailsText, Does.Contain(brand));
        Assert.That(detailsText, Does.Contain(model));
        Assert.That(detailsText, Does.Contain(plate));
        Assert.That(detailsText, Does.Contain("ACTIVO"));

        await CaptureStageAsync("consulta-detalle");
        await details.GetByRole(AriaRole.Button, new() { Name = "Cerrar" }).ClickAsync();
    }

    private async Task UpdateVehicleAsync(string plate, string updatedModel)
    {
        var row = VehicleRow(plate);
        await row.GetByRole(AriaRole.Button, new() { Name = "Editar" }).ClickAsync();

        var modelInput = await VehicleFieldAsync("Modelo *");
        await modelInput.FillAsync(updatedModel);
        await CaptureStageAsync("edicion-antes-de-guardar");

        await Page.Locator(".vehicle-form-card")
            .GetByRole(AriaRole.Button, new() { Name = "Guardar cambios" })
            .ClickAsync();
        await Page.GetByText("Vehículo actualizado correctamente.", new() { Exact = true })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });
    }

    private async Task VerifyUpdatedVehicleAsync(string plate, string brand, string updatedModel)
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Actualizar" }).ClickAsync();

        var row = VehicleRow(plate);
        await row.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var rowText = await row.InnerTextAsync();
        Assert.That(rowText, Does.Contain(brand));
        Assert.That(rowText, Does.Contain(updatedModel));

        await row.GetByRole(AriaRole.Button, new() { Name = "Ver" }).ClickAsync();
        var details = Page.Locator(".details-card");
        await details.GetByRole(AriaRole.Heading, new() { Name = "Detalle del vehículo" })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });
        Assert.That(await details.InnerTextAsync(), Does.Contain(updatedModel));

        await CaptureStageAsync("actualizacion-persistida");
        await details.GetByRole(AriaRole.Button, new() { Name = "Cerrar" }).ClickAsync();
    }

    private async Task ArchiveVehicleAsync(string plate)
    {
        var row = VehicleRow(plate);
        await row.GetByRole(AriaRole.Button, new() { Name = "Archivar" }).ClickAsync();

        var confirmation = Page.Locator(".archive-confirmation");
        await confirmation.GetByRole(
                AriaRole.Heading,
                new() { Name = "¿Archivar vehículo?" })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await confirmation.GetByRole(
                AriaRole.Button,
                new() { Name = "Confirmar archivado" })
            .ClickAsync();

        await Page.GetByText("Vehículo archivado correctamente.", new() { Exact = true })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await CaptureStageAsync("archivado-confirmado");
    }

    private async Task VerifyArchivedVehicleAsync(string plate)
    {
        // The current list contract retains logically archived vehicles.
        // Absence from ACTIVO alone does not prove a successful archive.
        await Page.GetByRole(AriaRole.Button, new() { Name = "Actualizar" }).ClickAsync();
        var row = VehicleRow(plate);
        await Microsoft.Playwright.Assertions.Expect(row.Locator(".status"))
            .ToHaveTextAsync("Archivado", new() { Timeout = E2EConfiguration.TimeoutMilliseconds });
        await OpenVehicleDetailsAsync(plate);
        await Microsoft.Playwright.Assertions.Expect(Page.Locator(".details-grid div")
                .Filter(new() { Has = Page.Locator("label").GetByText("Estado", new() { Exact = true }) })
                .Locator("strong"))
            .ToHaveTextAsync("ARCHIVADO", new() { Timeout = E2EConfiguration.TimeoutMilliseconds });

        await CaptureStageAsync("verificacion-archivado");
    }

}