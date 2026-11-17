/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: Program.cs
    File description: Program related implementation
    
    
    Created by: Senparc - 20241119
    
    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260724
    Change description: v0.0.5 Updated module behavior and related support.

----------------------------------------------------------------*/

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
        launchProfileName: "https")
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
