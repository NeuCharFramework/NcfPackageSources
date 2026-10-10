using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Senparc.Xncf.AgentsManagerTests.Application;

[TestClass]
public class AgentStudioRenderingTests
{
    [TestMethod]
    public async Task Studio_IndexRendersPartialAndVersionedAssets()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory,
            EnvironmentName = Environments.Development
        });
        builder.Environment.WebRootFileProvider =
            new ManifestEmbeddedFileProvider(typeof(AgentsManager.Register).Assembly, "wwwroot");
        builder.Services.AddLocalization();
        builder.Services.AddRazorPages().AddApplicationPart(typeof(AgentsManager.Register).Assembly);
        await using var application = builder.Build();
        using var scope = application.Services.CreateScope();
        var services = scope.ServiceProvider;
        var httpContext = new DefaultHttpContext { RequestServices = services };
        const string pagePath = "/Areas/Admin/Pages/AgentsManager/Index.cshtml";
        var factory = services.GetRequiredService<IRazorPageFactoryProvider>().CreateFactory(pagePath);
        Assert.IsTrue(factory.Success, "The compiled AgentsManager page was not found.");
        var page = factory.RazorPageFactory();
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        var context = new ViewContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            new RenderingView(),
            new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            new TempDataDictionary(httpContext, services.GetRequiredService<ITempDataProvider>()),
            writer,
            new HtmlHelperOptions())
        {
            ExecutingFilePath = pagePath
        };
        page.ViewContext = context;
        services.GetRequiredService<IRazorPageActivator>().Activate(page, context);
        Assert.IsInstanceOfType<Page>(page);
        ((Page)page).PageContext = new PageContext(context) { ViewData = context.ViewData };
        await page.ExecuteAsync();
        var body = writer.ToString();
        Assert.IsTrue(body.Contains("class=\"agent-studio\"", StringComparison.Ordinal),
            "The studio partial was not rendered.");
        Assert.IsTrue(body.Contains("ref=\"agent3dContainer\"", StringComparison.Ordinal),
            "The renderer's Vue container reference is missing.");
        Assert.IsFalse(body.Contains("<partial", StringComparison.OrdinalIgnoreCase),
            "A literal partial tag leaves the 3D branch empty in the browser.");

        writer.GetStringBuilder().Clear();
        await page.SectionWriters["Style"]();
        var styles = writer.ToString();
        Assert.IsTrue(styles.Contains("href=\"/css/AgentsManager/studio.css?v=", StringComparison.Ordinal));
        Assert.IsFalse(styles.Contains("asp-append-version", StringComparison.Ordinal));

        writer.GetStringBuilder().Clear();
        await page.SectionWriters["scripts"]();
        var scripts = writer.ToString();
        Assert.IsTrue(scripts.Contains("window.ncfI18n", StringComparison.Ordinal));
        Assert.IsTrue(scripts.Contains("src=\"/js/AgentsManager/three-loader.js?v=", StringComparison.Ordinal));
        Assert.IsFalse(scripts.Contains("<partial", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(scripts.Contains("asp-append-version", StringComparison.Ordinal));

        var artifactPath = Environment.GetEnvironmentVariable("NCF_STUDIO_RENDERED_PAGE");
        if (!string.IsNullOrWhiteSpace(artifactPath))
        {
            await File.WriteAllTextAsync(artifactPath,
                System.Text.Json.JsonSerializer.Serialize(new { body, styles, scripts }));
        }
    }

    private sealed class RenderingView : IView
    {
        public string Path => "/Areas/Admin/Pages/AgentsManager/Index.cshtml";

        public Task RenderAsync(ViewContext context) => throw new NotSupportedException();
    }
}
