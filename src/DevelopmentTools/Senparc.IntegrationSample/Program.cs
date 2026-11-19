/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Program.cs
    File description: Program related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.0.19 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/


using Senparc.Ncf.Database.SqlServer;

using Senparc.IntegrationSample;


var builder = WebApplication.CreateBuilder(args);

//Legacy comment translated to English.
builder.AddNcf();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

//Use NCF（must）
app.UseNcf<BySettingDatabaseConfiguration>();
//var col = Senparc.Ncf.Core.Models.EntitySetKeys.GetAllEntitySetInfo();
//Console.WriteLine("================================EntitySetKeys:");
//Console.WriteLine(col.Where(z => z.Key.FullName.Contains("AI")).ToJson(true));

app.UseStaticFiles();

app.UseCookiePolicy();

app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.ShowSuccessTip();

app.Run();
