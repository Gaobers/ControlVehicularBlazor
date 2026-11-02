using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Playwright;
using NUnit.Framework;

namespace ControlVehicularPortalWeb.E2E;

public abstract class VehicleE2ETestBase : E2ETestBase
{
    protected sealed record RemoteWriteConfiguration(string Email, string Password, long OwnerId);

    protected sealed record VehicleTestData(
        string Brand,
        string Model,
        string Plate,
        string Color,
        short Year,
        decimal Mileage);

    protected IPlaywright Playwright => BrowserPlaywright;

    protected RemoteWriteConfiguration? GetRemoteWriteConfiguration()
    {
        var email = Environment.GetEnvironmentVariable("E2E_EMAIL");
        var password = Environment.GetEnvironmentVariable("E2E_PASSWORD");
        var ownerValue = Environment.GetEnvironmentVariable("E2E_OWNER_ID");

        if (!E2EConfiguration.AllowRemoteWrites
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password)
            || !long.TryParse(ownerValue, out var ownerId)
            || ownerId < 1)
        {
            CaptureArtifacts = false;
            return null;
        }

        return new RemoteWriteConfiguration(email, password, ownerId);
    }

    protected static VehicleTestData CreateUniqueVehicle(string casePrefix)
    {
        var token = $"{DateTime.UtcNow:MMddHHmmssfff}{RandomNumberGenerator.GetInt32(0, 1_000_000):D6}";
        return new VehicleTestData(
            Brand: $"{casePrefix}{token}",
            Model: $"Modelo{token}",
            Plate: $"P{token}",
            Color: "Azul QA",
            Year: (short)DateTime.UtcNow.Year,
            Mileage: 1200m);
    }

    protected async Task AuthenticateAsync(string email, string password, string caseId = "CV-60")
    {
        await DiscardCurrentTraceAsync();
        await Page.GotoAsync($"{E2EConfiguration.BaseUrl}/login");
        await Page.GetByLabel("Correo electrónico").FillAsync(email);
        await Page.GetByLabel("Contraseña").FillAsync(password);

        var loginSuccess = Page.GetByRole(
            AriaRole.Heading,
            new() { Name = "Sesión iniciada" });
        var loginError = Page.GetByRole(AriaRole.Alert);

        await Page.GetByLabel("Contraseña").PressAsync("Enter");
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.login-success h2')?.textContent?.trim() === 'Sesión iniciada' " +
            "|| Boolean(document.querySelector('.error-alert[role=alert]')?.textContent?.trim())",
            null,
            new() { Timeout = E2EConfiguration.TimeoutMilliseconds });

        if (!await loginSuccess.IsVisibleAsync())
        {
            var message = await loginError.InnerTextAsync();
            Assert.Fail($"El portal no confirmó la autenticación. Mensaje visible: {message}");
        }

        await CaptureStageAsync("inicio-sesion", caseId);
        await StartTracingAsync();
    }

    protected async Task OpenVehicleModuleAsync(string caseId = "CV-60")
    {
        await Page.GetByRole(AriaRole.Link, new() { Name = "Ir a Gestión de Vehículos →" }).ClickAsync();
        await Page.GetByRole(AriaRole.Heading, new() { Name = "Gestión de Vehículos" })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });

        await Page.SetViewportSizeAsync(800, 900);
        var menuToggle = Page.GetByRole(
            AriaRole.Button,
            new() { Name = "Mostrar u ocultar navegación" });
        await menuToggle.ClickAsync();
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.menu-toggle')?.getAttribute('aria-expanded') === 'true'");
        await menuToggle.ClickAsync();
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.menu-toggle')?.getAttribute('aria-expanded') === 'false'");

        Assert.That(
            await Page.GetByRole(AriaRole.Button, new() { Name = "Actualizar" }).IsVisibleAsync(),
            Is.True,
            "El módulo debe mostrar la acción para actualizar la consulta.");
        Assert.That(
            await Page.Locator(".page-header").GetByRole(
                AriaRole.Button,
                new() { Name = "Registrar vehículo" }).IsVisibleAsync(),
            Is.True,
            "El módulo debe mostrar la acción para registrar vehículos.");

        await CaptureStageAsync("modulo-vehiculos", caseId);
    }

    protected async Task WaitForVehicleListAsync()
    {
        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('.vehicles-table tbody tr').length > 0 " +
            "|| document.querySelector('.table-message h3')?.textContent?.trim() === 'No se encontraron vehículos' " +
            "|| Boolean(document.querySelector('.error-message'))",
            null,
            new() { Timeout = E2EConfiguration.TimeoutMilliseconds });

        Assert.That(await Page.Locator(".error-message").CountAsync(), Is.Zero,
            "La carga de vehículos no debe terminar en error.");
    }

    protected async Task FillVehicleFieldAsync(string label, string value)
    {
        await (await VehicleFieldAsync(label)).FillAsync(value);
    }

    protected async Task<ILocator> VehicleFieldAsync(string label)
    {
        var form = Page.Locator(".vehicle-form-card");
        await form.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        var labelElement = form.Locator("label")
            .Filter(new()
            {
                HasTextString = label.Replace("*", string.Empty, StringComparison.Ordinal).Trim()
            });
        await labelElement.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var field = labelElement
            .Locator("xpath=..")
            .Locator("input");
        await field.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        Assert.That(
            await field.CountAsync(),
            Is.EqualTo(1),
            $"Debe existir exactamente un campo para '{label}'.");

        return field;
    }

    protected async Task AssertYearRangeValidationAsync(ILocator yearField, string caseId)
    {
        Assert.That(
            await yearField.EvaluateAsync<bool>("element => element.validity.rangeUnderflow"),
            Is.True,
            "El input Año debe aplicar el límite mínimo HTML real de 1900.");

        await CaptureStageAsync("validacion-anio-fuera-de-rango", caseId);
    }

    protected async Task AssertYearRangeMessageAsync(ILocator yearField)
    {
        var validation = yearField.Locator("xpath=..").Locator(".validation-message");
        await validation.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var message = await validation.InnerTextAsync();
        Assert.That(message, Does.Contain("1900"));
        Assert.That(message, Does.Contain("2200"));
    }

    protected ILocator VehicleRow(string plate) =>
        Page.Locator(".vehicles-table tbody tr")
            .Filter(new() { HasTextString = plate });

    protected async Task<long> ReadVehicleIdFromRowAsync(ILocator row)
    {
        var idText = (await row.Locator("td").First.InnerTextAsync()).Trim().TrimStart('#');
        Assert.That(long.TryParse(idText, out var vehicleId), Is.True,
            "La primera celda del registro debe contener un identificador numérico.");
        Assert.That(vehicleId, Is.GreaterThan(0));
        return vehicleId;
    }

    protected async Task<string> ReadVehicleDetailFieldAsync(string label)
    {
        var labelElement = Page.Locator(".details-grid label")
            .GetByText(label, new() { Exact = true });
        var value = labelElement.Locator("xpath=..").Locator("strong");
        Assert.That(await value.CountAsync(), Is.EqualTo(1),
            $"El detalle debe mostrar exactamente un valor para '{label}'.");
        return (await value.InnerTextAsync()).Trim();
    }

    protected async Task OpenVehicleDetailsAsync(string plate)
    {
        await VehicleRow(plate).GetByRole(AriaRole.Button, new() { Name = "Ver" }).ClickAsync();
        await Page.Locator(".details-card")
            .GetByRole(AriaRole.Heading, new() { Name = "Detalle del vehículo" })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });
    }

    protected async Task RegisterVehicleAsync(
        RemoteWriteConfiguration config,
        VehicleTestData vehicle,
        string caseId)
    {
        await Page.Locator(".page-header").GetByRole(
            AriaRole.Button,
            new() { Name = "Registrar vehículo" }).ClickAsync();

        await FillVehicleFieldAsync("Propietario ID *", config.OwnerId.ToString());
        await FillVehicleFieldAsync("Marca *", vehicle.Brand);
        await FillVehicleFieldAsync("Modelo *", vehicle.Model);
        await FillVehicleFieldAsync("Año *", vehicle.Year.ToString());
        await FillVehicleFieldAsync("Placa *", vehicle.Plate);
        await FillVehicleFieldAsync("Color", vehicle.Color);
        await FillVehicleFieldAsync("Kilometraje actual *", vehicle.Mileage.ToString());

        var form = Page.Locator(".vehicle-form-card");
        await form.GetByRole(AriaRole.Button, new() { Name = "Registrar vehículo" }).ClickAsync();
        await Page.GetByText("Vehículo registrado correctamente.", new() { Exact = true })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });

        var row = VehicleRow(vehicle.Plate);
        await row.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        Assert.That(await row.InnerTextAsync(), Does.Contain(vehicle.Brand));
        Assert.That(await row.InnerTextAsync(), Does.Contain(vehicle.Model));
        Assert.That(await row.InnerTextAsync(), Does.Contain(vehicle.Plate));
        await CaptureStageAsync("vehiculo-registrado", caseId);
    }

    protected async Task ArchiveVehicleAsync(string plate, string caseId)
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

        var archivedRow = VehicleRow(plate);
        await archivedRow.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        Assert.That(await archivedRow.Locator(".status").InnerTextAsync(), Is.EqualTo("Archivado"));
        await CaptureStageAsync("vehiculo-archivado", caseId);
    }

    protected async Task<(IAPIRequestContext Context, string Token)> CreateAuthenticatedApiContextAsync(
        string email,
        string password)
    {
        var api = await Playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            BaseURL = E2EConfiguration.ApiBaseUrl,
            IgnoreHTTPSErrors = true,
            Timeout = E2EConfiguration.TimeoutMilliseconds
        });

        var loginResponse = await api.PostAsync("api/auth/login", new APIRequestContextOptions
        {
            Data = JsonSerializer.Serialize(new { correo = email, clave = password }),
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            }
        });

        if (loginResponse.Status != 200)
        {
            await api.DisposeAsync();
            Assert.Fail($"El login API complementario devolvió HTTP {loginResponse.Status}.");
        }

        using var document = JsonDocument.Parse(await loginResponse.TextAsync());
        var token = document.RootElement.TryGetProperty("token", out var tokenValue)
            ? tokenValue.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(token))
        {
            await api.DisposeAsync();
            Assert.Fail("El endpoint de login API no devolvió un JWT.");
        }

        return (api, token!);
    }
}
