using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.CO2NET.AspNet;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.SqlServer;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.Accounts.Domain.Models;
using Senparc.Xncf.Accounts.Models;
using Senparc.Xncf.AreasBase;
using System;
using System.IO;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
//builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddDaprClient();
builder.Services.AddControllers().AddDapr();

//Activate Xncf extension engine (required)
var logMsg = builder.StartWebEngine(new[] { "Senparc.Areas.Admin" });
//If you don't need to enable Areas, you can just use the services.StartEngine() method

Console.WriteLine("============ logMsg =============");
Console.WriteLine(logMsg);
Console.WriteLine("============ logMsg END =============");


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
//Use NCF (required)
IOptions<SenparcCoreSetting> senparcCoreSetting = app.Services.GetService<IOptions<SenparcCoreSetting>>();

//Start CO2NET global registration, must!
//For more usage of UseSenparcGlobal(), see CO2NET Demo: https://github.com/Senparc/Senparc.CO2NET/blob/master/Sample/Senparc.CO2NET.Sample.netcore3/Startup.cs
var registerService = app.UseSenparcGlobal(app.Environment);

//XncfModules (required)
app.UseXncfModules(registerService, senparcCoreSetting.Value)
//Specify database (required)
   .UseNcfDatabase<SqlServerDatabaseConfiguration>();

using (var scope = app.Services.CreateScope())
{
    foreach (var register in Senparc.Ncf.XncfBase.XncfRegisterManager.RegisterList)
    {
        await register.InstallOrUpdateAsync(scope.ServiceProvider, Senparc.Ncf.Core.Enums.InstallOrUpdate.Install);
    }
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCookiePolicy();

app.UseRouting();
app.UseAuthorization();

app.Map("/Test2", _app =>
{
    _app.Use(async (HttpContext context, RequestDelegate next) =>
    {
        var sr = new StreamWriter(context.Response.Body);
        await sr.WriteLineAsync("OK");
        await sr.FlushAsync();
    });
});

app.Map("/TestDB", _app =>
{
    _app.Use(async (HttpContext context, RequestDelegate next) =>
    {
        using (var scope = context.RequestServices.CreateScope())
        {
            var accountCount = 0;
            try
            {
                var dbContextType = MultipleDatabasePool.Instance.GetXncfDbContextType(typeof(Senparc.Xncf.Accounts.Register));
                var ctx = scope.ServiceProvider.GetService(dbContextType) as AccountSenparcEntities;
                accountCount = ctx.Set<Account>().Count();
                Console.WriteLine("accountCount:" + accountCount);
            }
            catch (global::System.Exception ex)
            {
                global::System.Console.WriteLine(ex.ToString());
            }
            finally
            {
                var sr = new StreamWriter(context.Response.Body);
                await sr.WriteLineAsync("accountCount:" + accountCount);
                await sr.FlushAsync();
            }
        }
    });
});

app.MapRazorPages();
app.MapControllers();

//app.MapDefaultEndpoints();

app.Run();
