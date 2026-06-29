using Aspire.Hosting;
using Projects;
using Senparc.Ncf.Core.WebApi;
using System.Text;

var builder = DistributedApplication.CreateBuilder(args);

var installer = builder.AddProject<Senparc_Xncf_Installer>(
        NcfWebApiHelper.GetXncfProjectName<Senparc_Xncf_Installer>(),
        launchProfileName: "https")
    .WithExternalHttpEndpoints();

var accounts = builder.AddProject<Senparc_Xncf_Accounts>(
        NcfWebApiHelper.GetXncfProjectName<Senparc_Xncf_Accounts>(),
        launchProfileName: "Senparc.Xncf.Accounts")
    .WithExternalHttpEndpoints();

var ncfWeb = builder.AddProject<Senparc_Web>(
        NcfWebApiHelper.GetXncfProjectName<Senparc_Web>(),
        launchProfileName: "http")
    .WithReference(installer)
    .WithReference(accounts)
    .WithExternalHttpEndpoints();

// Keep legacy code page support used by existing modules.
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

builder.Build().Run();
