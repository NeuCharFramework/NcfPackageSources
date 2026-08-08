using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Senparc.Ncf.AreaBase.Admin.Filters;

namespace Senparc.Areas.Admin.Tests;

[TestClass]
public class AdminAuthenticationConfigurationTests
{
    [TestMethod]
    public void AuthorizeConfig_AllowsLocalHttpLoginWithoutWeakeningHttpsCookies()
    {
        var services = new ServiceCollection();
        var mvcBuilder = services.AddRazorPages();
        var environment = new Mock<IHostEnvironment>().Object;

        new Senparc.Areas.Admin.Register().AuthorizeConfig(mvcBuilder, environment);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(AdminAuthorizeAttribute.AuthenticationScheme);

        Assert.IsTrue(options.Cookie.HttpOnly);
        Assert.AreEqual(SameSiteMode.Strict, options.Cookie.SameSite);
        Assert.AreEqual(CookieSecurePolicy.SameAsRequest, options.Cookie.SecurePolicy);
    }
}
