/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：XncfPreviewRequest.cs
    文件功能描述：XNCF 独立预览请求模型


    创建标识：Senparc - 20260801

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.XncfBuilder.Domain.Services.Preview;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.OHS.PL
{
    public class BuildXncf_PreviewRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(250)]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.SolutionPath")]
        public string SlnFilePath { get; set; }

        [Required]
        [MaxLength(150)]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Preview.ModuleProjectName")]
        public string ModuleProjectName { get; set; }

        [Range(0, 65535)]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Preview.Port")]
        public int Port { get; set; }

        [Range(10, 600)]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Preview.Timeout")]
        public int StartupTimeoutSeconds { get; set; } = 120;

        [MaxLength(50)]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Preview.Environment")]
        public string EnvironmentName { get; set; } = XncfPreviewService.DefaultEnvironmentName;

        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrWhiteSpace(SlnFilePath))
            {
                SlnFilePath = FindSolutionFilePath();
            }

            try
            {
                var configService = serviceProvider.GetService<ServiceBase<Config>>();
                var config = configService == null
                    ? null
                    : await configService.GetObjectAsync(z => true).ConfigureAwait(false);
                if (config != null)
                {
                    SlnFilePath = string.IsNullOrWhiteSpace(SlnFilePath) ? config.SlnFilePath : SlnFilePath;
                    ModuleProjectName = $"{config.OrgName}.Xncf.{config.XncfName}";
                }
            }
            catch
            {
                // Loading the last Builder configuration is optional for the preview form.
            }
        }

        private static string FindSolutionFilePath()
        {
            var currentDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (currentDirectory != null)
            {
                var solutionFile = currentDirectory
                    .EnumerateFiles("*.sln")
                    .OrderBy(z => z.FullName.Length)
                    .ThenBy(z => z.FullName, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (solutionFile != null)
                {
                    return solutionFile.FullName;
                }

                currentDirectory = currentDirectory.Parent;
            }

            return string.Empty;
        }
    }

    public class BuildXncf_PreviewStatusRequest : FunctionAppRequestBase
    {
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Preview.IncludeOutput")]
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(IncludeOutputOptions))]
        public bool IncludeOutput { get; set; }

        [JsonIgnore]
        public SelectionList IncludeOutputOptions { get; set; } = new(
            SelectionType.CheckBoxList,
            new[]
            {
                new SelectionItem(
                    "1",
                    XncfBuilderResource.Get("XncfBuilder.Option.Preview.IncludeOutput"),
                    XncfBuilderResource.Get("XncfBuilder.Option.Preview.IncludeOutput.Help"),
                    false)
            });
    }

    public class BuildXncf_StopPreviewRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(150)]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Preview.SessionId")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(SessionIdOptions))]
        public string SessionId { get; set; }

        [JsonIgnore]
        public SelectionList SessionIdOptions { get; set; } = new(
            SelectionType.DropDownList,
            Array.Empty<SelectionItem>());

        public override Task LoadData(IServiceProvider serviceProvider)
        {
            var previewService = serviceProvider.GetService<IXncfPreviewService>();
            var sessions = previewService?.GetSessions() ?? Array.Empty<XncfPreviewSessionInfo>();
            SessionIdOptions = new SelectionList(
                SelectionType.DropDownList,
                sessions.Select(z => new SelectionItem(
                    z.SessionId,
                    $"{z.ModuleProjectName} ({z.Url})",
                    z.IsRunning ? XncfBuilderResource.Get("XncfBuilder.Preview.Running") : XncfBuilderResource.Get("XncfBuilder.Preview.Stopped"),
                    false)).ToArray());
            SessionId ??= sessions.FirstOrDefault(z => z.IsRunning)?.SessionId;
            return Task.CompletedTask;
        }
    }
}
