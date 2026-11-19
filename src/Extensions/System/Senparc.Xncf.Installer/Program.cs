/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Program.cs
    File description: Program related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260729
    Change description: v0.4.1 Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.5.14 Updated module behavior and related support.

----------------------------------------------------------------*/


using Microsoft.AspNetCore.Builder;

using Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Localization;

using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Hosting;

using Microsoft.Extensions.Options;

using Senparc.CO2NET;

using Senparc.CO2NET.AspNet;

using Senparc.CO2NET.HttpUtility;

using Senparc.Ncf.Core.Areas;

using Senparc.Ncf.Core.Localization;

using Senparc.Ncf.Core.Models;

using Senparc.Ncf.Core.WebApi;

using Senparc.Ncf.Database;

using Senparc.Ncf.Database.SqlServer;

using Senparc.Ncf.XncfBase;

using Senparc.Ncf.Utility.Helpers;

using Senparc.Xncf.AreasBase;

using System;

using System.Linq;

using System.Text;



var builder = WebApplication.CreateBuilder(args);



// Add service defaults & Aspire components.
builder.AddServiceDefaults();


// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddNcfLocalization();


builder.Services.AddControllers().AddDapr();


//Legacy comment translated to English.
var logMsg = builder.StartWebEngine(new[] { "Senparc.Areas.Admin" });

//Legacy comment translated to English.


Console.WriteLine("============ logMsg =============");

Console.WriteLine(logMsg);

Console.WriteLine("============ logMsg END =============");



builder.Services.AddSwaggerGen();



var app = builder.Build();



if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

var supportedCultures = NcfLocalizationOptions.SupportedCultures;

app.UseRequestLocalization(new RequestLocalizationOptions()

    .SetDefaultCulture(NcfLocalizationOptions.DefaultCulture)

    .AddSupportedCultures(supportedCultures.ToArray())

    .AddSupportedUICultures(supportedCultures.ToArray())

    .AddInitialRequestCultureProvider(new CookieRequestCultureProvider()));



//Legacy comment translated to English.
app.UseSenparcMvcDI();

//Use NCF（must）
IOptions<SenparcCoreSetting> senparcCoreSetting = app.Services.GetService<IOptions<SenparcCoreSetting>>();
IOptions<SenparcSetting> senparcSetting = app.Services.GetService<IOptions<SenparcSetting>>();


// start CO2NET global registration，must！

//Legacy comment translated to English.
var registerService = app

    //global registration

    .UseSenparcGlobal(app.Environment, senparcSetting.Value, globalRegister => { });





//XncfModules（must）
app.UseXncfModules(registerService, senparcCoreSetting.Value)
   .UseNcfDatabase<BySettingDatabaseConfiguration>();
//using (var scope = app.Services.CreateScope())

//{



//    foreach (var register in Senparc.Ncf.XncfBase.XncfRegisterManager.RegisterList)

//    {

//        await register.InstallOrUpdateAsync(scope.ServiceProvider, Senparc.Ncf.Core.Enums.InstallOrUpdate.Install);

//    }

//}



// These routes are local Aspire/Dapr smoke-test helpers. Do not publish them from
// the installer host in a non-development environment.
if (app.Environment.IsDevelopment())
{
    app.Map("/TestInstall", () => ("this is a text from Dapr Container." + SystemTime.Now));
}


//app.UseHttpsRedirection();

app.UseStaticFiles();



app.UseCookiePolicy();



app.UseRouting();

app.UseAuthorization();



app.MapRazorPages();
app.MapControllers();

app.MapDefaultEndpoints();


if (app.Environment.IsDevelopment())
{
    app.MapGet("/aspire-test1", async httpContext =>
    {
        var xncfName = "Senparc.Xncf.Accounts";//Assembly name / catalog

        var apiClientHelper = httpContext.RequestServices.GetService<ApiClientHelper>();
        var apiClient = apiClientHelper.ConnectApiClient(NcfWebApiHelper.GetXncfProjectName(xncfName));

        var apiBindName = "AccountAppService";
        var methodName = "KeepAlive";
        var apiPath = NcfWebApiHelper.GetNcfApiClientPath(xncfName, apiBindName, methodName, null);
        Console.WriteLine("/test url: " + apiPath);
        //var apiPath = $"/api/{keyName}/{apiBindGroupNamePath}/{apiNamePath}{showStaticApiState}";
        var url = apiPath;
        var result2 = await RequestUtility.HttpGetAsync(null, url, Encoding.UTF8, apiClient);

        await httpContext.Response.WriteAsync(result2);
    });

    app.MapGet("/aspire-test2", async httpContext =>
    {
        var xncfName = "Senparc.Xncf.Installer";//Assembly name / catalog
        var apiClientHelper = httpContext.RequestServices.GetService<ApiClientHelper>();
        var apiClient = apiClientHelper.ConnectApiClient(NcfWebApiHelper.GetXncfProjectName(xncfName));
        var url = "/TestInstall";
        var result2 = await RequestUtility.HttpGetAsync(null, url, Encoding.UTF8, apiClient);

        await httpContext.Response.WriteAsync(result2);
    });
}


app.Run();

