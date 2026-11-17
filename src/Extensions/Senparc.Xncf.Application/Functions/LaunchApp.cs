/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: LaunchApp.cs

    File description: LaunchApp related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



    Modified by: Senparc - 20260717

    Change description: v0.1.0 integrate shared resource localization and improve feature text for the Application module



----------------------------------------------------------------*/

using Senparc.CO2NET.Extensions;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;

namespace Senparc.Xncf.Application.Functions
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class LaunchApp : FunctionBase
    {
        public class LaunchApp_Parameters : IFunctionParameter
        {
        //    /// <summary>
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //    /// </summary>
        //    [Required]
        //Legacy comment translated to English.
        //    public string[] LaunchPath { get; set; } = new[] {
        //    Parameters_Path.System.ToString(),
        //    Parameters_Path.Custom.ToString()
        //};

        //    public enum Parameters_Path
        //    {
        //        System,
        //        Custom
        //    }

            [Required]
            [MaxLength(300)]
            [LocalizedDescription(typeof(ApplicationResource), "Parameter.Application.FilePath")]
            public string FilePath { get; set; }
        }

        //Legacy comment translated to English.
        public override string Name => ApplicationResource.Get("Function.Application.Launch.Name");

        public override string Description => ApplicationResource.Get("Function.Application.Launch.Description");

        public override Type FunctionParameterType => typeof(LaunchApp_Parameters);

        public LaunchApp(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// run
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public override FunctionResult Run(IFunctionParameter param)
        {
            var typeParam = param as LaunchApp_Parameters;

            FunctionResult result = new FunctionResult()
            {
                Success = true
            };

            StringBuilder sb = new StringBuilder();
            base.RecordLog(sb, ApplicationResource.Get("Function.Application.Launch.StartLog"));

            StartApp(typeParam.FilePath);

            //sb.AppendLine($"LaunchPath{typeParam.LaunchPath}");
            sb.AppendLine($"FilePath{typeParam.FilePath}");

            result.Log = sb.ToString();
            result.Message = ApplicationResource.Get("Common.OperationSucceeded");
            return result;
        }

        #region Legacy section
        private bool StartApp(string appName)
        {
            return StartApp(appName, ProcessWindowStyle.Hidden);
        }

        private bool StartApp(string appName, ProcessWindowStyle style)
        {
            return StartApp(appName, null, style);
        }

        private bool StartApp(string appName, string arguments)
        {
            return StartApp(appName, arguments, ProcessWindowStyle.Hidden);
        }

        private void StartAppointApp(string filePath, string fileName)
        {
            Process exep = Process.Start(filePath + fileName);
        }

        private void StartAppointApp(string fileNamePath)
        {
            Process exep = Process.Start(fileNamePath);
        }

        private bool StartApp(string appName, string arguments, ProcessWindowStyle style)
        {
            bool blnRst = false;
            Process p = new Process();
            p.StartInfo.FileName = appName;//exe,bat and so on
            p.StartInfo.WindowStyle = style;
            p.StartInfo.Arguments = arguments;
            try
            {
                p.Start();
                p.WaitForExit();
                p.Close();
                blnRst = true;
            }
            catch
            {
            }
            return blnRst;
        }
        #endregion

    }
}
