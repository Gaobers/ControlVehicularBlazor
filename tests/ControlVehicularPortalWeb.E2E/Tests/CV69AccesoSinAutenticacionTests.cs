using Allure.NUnit;
using Allure.NUnit.Attributes;
using Microsoft.Playwright;
using NUnit.Framework;

namespace ControlVehicularPortalWeb.E2E.Tests;

[TestFixture]
[Category("CV-69")]
[AllureNUnit]
[NonParallelizable]
public sealed class CV69AccesoSinAutenticacionTests : E2ETestBase
{
    [Test]
    [AllureFeature("Seguridad del portal")]
    [AllureStory("CV-69 - Rechazo de acceso sin autenticación")]
    [AllureName("CV-69 - Bloquear gestión de vehículos sin sesión")]
    public async Task Vehiculos_RedireccionaALoginYNoExponeGestion_SinAutenticacion()
    {
        var protectedUrl = new Uri(new Uri($"{E2EConfiguration.BaseUrl}/"), "vehiculos");

        await Page.GotoAsync(protectedUrl.ToString());
        if (new Uri(Page.Url).AbsolutePath != "/login")
        {
            await Page.WaitForURLAsync("**/login");
        }

        Assert.That(new Uri(Page.Url).AbsolutePath, Is.EqualTo("/login"),
            "La ruta protegida debe redirigir la sesión anónima al login.");

        Assert.That(
            await Page.GetByRole(AriaRole.Heading, new() { Name = "Iniciar sesión" }).IsVisibleAsync(),
            Is.True,
            "Debe mostrarse el formulario real de autenticación del portal.");

        Assert.That(
            await Page.GetByRole(AriaRole.Button, new() { Name = "Iniciar sesión" }).IsVisibleAsync(),
            Is.True,
            "La pantalla de login debe permitir iniciar sesión, no gestionar vehículos.");

        Assert.That(
            await Page.GetByRole(AriaRole.Heading, new() { Name = "Gestión de Vehículos" }).CountAsync(),
            Is.Zero,
            "No debe renderizarse el encabezado del módulo protegido.");

        Assert.That(
            await Page.GetByRole(AriaRole.Button, new() { Name = "Registrar vehículo" }).CountAsync(),
            Is.Zero,
            "No debe estar disponible la acción para registrar vehículos.");

        Assert.That(
            await Page.GetByRole(AriaRole.Button, new() { Name = "Guardar cambios" }).CountAsync(),
            Is.Zero,
            "No debe estar disponible la acción para actualizar vehículos.");
    }
}