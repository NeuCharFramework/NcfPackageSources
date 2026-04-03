using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions.Parameters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.Functions
{
    /// <summary>
    ///Function helper class
    /// </summary>
    public class FunctionHelper
    {
        /// <summary>
        ///log
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="msg"></param>
        public static void RecordLog(StringBuilder sb, string msg)
        {
            sb.AppendLine($"[{SystemTime.Now.ToString()}]\t{msg}");
        }

        /// <summary>
        /// Get the information list of all parameters
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="functionRenderBag">FunctionRenderBag</param>
        /// <param name="tryLoadData">Whether to try to load data (the parameter must implement the IFunctionParameterLoadDataBase interface)</param>
        /// <returns></returns>
        public static async Task<List<FunctionParameterInfo>> GetFunctionParameterInfoAsync(IServiceProvider serviceProvider, FunctionRenderBag functionRenderBag, bool tryLoadData = true)
        {
            var functionParameterType = functionRenderBag.FunctionParameterType;

            IAppRequest paraObj = null;

            if (ReflectionHelper.HasParameterlessConstructor(functionParameterType))
            {
                //Contains a constructor with no parameters
                paraObj = functionParameterType.Assembly.CreateInstance(functionParameterType.FullName) as IAppRequest;//TODO: generated through DI

                if (tryLoadData && paraObj is FunctionAppRequestBase functionRequestPara)
                {
                    try
                    {
                        await functionRequestPara.LoadData(serviceProvider);//Load original information
                    }
                    catch (Exception ex)
                    {
                        SenparcTrace.BaseExceptionLog(ex);
                        throw;
                    }
                }
            }

            var props = functionParameterType.GetProperties();
            ParameterType parameterType = ParameterType.Text;
            List<FunctionParameterInfo> result = new List<FunctionParameterInfo>();
            foreach (var prop in props)
            {
                SelectionList selectionList = null;
                parameterType = ParameterType.Text;//Defaults to text content
                                                   //Determine if option exists
                if (prop.PropertyType == typeof(SelectionList))
                {
                    var selections = prop.GetValue(paraObj, null) as SelectionList;
                    switch (selections.SelectionType)
                    {
                        case SelectionType.DropDownList:
                            parameterType = ParameterType.DropDownList;
                            break;
                        case SelectionType.CheckBoxList:
                            parameterType = ParameterType.CheckBoxList;
                            break;
                        default:
                            //TODO: throw
                            break;
                    }
                    selectionList = selections;
                }
                else if (prop.Name.Contains("SECRET", StringComparison.OrdinalIgnoreCase) || prop.GetCustomAttribute<PasswordAttribute>() != null)
                {
                    parameterType = ParameterType.Password;
                }

                var name = prop.Name;
                string title = null;
                string description = null;
                var isRequired = prop.GetCustomAttribute<RequiredAttribute>() != null;
                var descriptionAttr = prop.GetCustomAttribute<DescriptionAttribute>();
                var maxLengthAttr = prop.GetCustomAttribute<MaxLengthAttribute>();
                var maxLength = 0;
                if (descriptionAttr != null && descriptionAttr.Description != null)
                {
                    //Split: Name||Description
                    var descriptionAttrArr = descriptionAttr.Description.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                    title = descriptionAttrArr[0];
                    if (descriptionAttrArr.Length > 1)
                    {
                        description = descriptionAttrArr[1];
                    }
                }

                if (maxLengthAttr != null)
                {
                    maxLength = maxLengthAttr.Length;
                }

                var systemType = prop.PropertyType.Name;

                object value = null;
                try
                {
                    value = prop.GetValue(paraObj);
                }
                catch (Exception ex)
                {
                    SenparcTrace.BaseExceptionLog(ex);
                }

                var functionParamInfo = new FunctionParameterInfo(name, title, description, isRequired, systemType, parameterType,
                                                selectionList ?? new SelectionList(SelectionType.Unknown), value, maxLength);
                result.Add(functionParamInfo);
            }
            return result;
        }

        /// <summary>
        /// Add the XNCF items in the current solution to the SelectionList object
        /// (Scan all domain projects contained in the current solution)
        /// </summary>
        /// <param name="mustHaveXncfModule">Whether the current solution must contain the XNCF project</param>
        /// <param name="rootDir">Find the XNCF module root directory. If left null, use <code>System.IO.Directory.GetCurrentDirectory()</code> to obtain it, and find the folder where the .sln is located</param>
        /// <param name="additionalProjects">Additional projects besides standard XNCF</param>
        public static List<SelectionItem> LoadXncfProjects(bool mustHaveXncfModule = false, string rootDir = null, params string[] additionalProjects)
        {
            var selectList = new List<SelectionItem>();

            var currentDir = rootDir ?? System.IO.Directory.GetCurrentDirectory();

            while (currentDir != null)
            {
                var slnFile = Directory.GetFiles(currentDir, "*.sln");
                if (slnFile.Length > 0)
                {
                    break;
                }
                currentDir = Directory.GetParent(currentDir).FullName;
            }

            if (currentDir != null)
            {
                //Found the SLN file and started searching

                //Step 1: Find XNCF

                var projectFolders = Directory.GetDirectories(currentDir, "*.XNCF.*", SearchOption.AllDirectories).ToList();

                if (additionalProjects != null && additionalProjects.Length > 0)
                {
                    foreach (var proj in additionalProjects)
                    {
                        var addProjectFolders = Directory.GetDirectories(currentDir, proj, SearchOption.AllDirectories);
                        if (addProjectFolders.Count() > 0)
                        {
                            projectFolders.AddRange(addProjectFolders);
                        }
                    }
                }

                foreach (var projectFolder in projectFolders)
                {
                    //Step 2: Check whether the Register file exists
                    var registerFilePath = Path.Combine(projectFolder, "Register.cs");
                    if (!File.Exists(registerFilePath))
                    {
                        continue;//If it does not exist, skip it.
                    }

                    //Step 3: Check whether the Register file is qualified

                    var registerContent = File.ReadAllText(registerFilePath);
                    if (registerContent.Contains("[XncfRegister]") &&
                        registerContent.Contains("IXncfRegister") &&
                        registerContent.Contains("Uid"))
                    {
                        selectList.Add(
                            new SelectionItem(
                                projectFolder,
                                projectFolder, /*Path.GetFileName(projectFolder)*/
                                projectFolder/*Path.GetDirectoryName(projectFolder)*/));
                    }
                }
            }

            if (mustHaveXncfModule && (currentDir == null || selectList.Count == 0))
            {
                selectList.Add(
                             new SelectionItem(
                                 "N/A",
                                 "没有发现任何可用的 XNCF 项目，请确保你正在一个标准的 NCF 开发环境中！"));
            }

            return selectList;
        }

        /// <summary>
        /// Get xxxSenparcEntities.cs database file
        /// </summary>
        /// <param name="projectPath"></param>
        /// <param name="dbType">Database type</param>
        /// <returns></returns>
        /// <exception cref="NcfExceptionBase"></exception>
        public static string GetSenparcEntitiesFilePath(string projectPath, string dbType)
        {
            var databaseModelPath = Path.Combine(projectPath, "Domain", "Models", "DatabaseModel");
            var files = Directory.GetFiles(databaseModelPath, "*SenparcEntities.cs");
            if (files.Length == 0)
            {
                throw new NcfExceptionBase($"目录 {databaseModelPath} 下没有找到 SenparcEntities.cs 结尾的文件");
            }

            var databaseFile = Path.GetFileName(files[0]).Replace(".cs", "");

            if (!dbType.IsNullOrEmpty())
            {
                databaseFile += "_" + dbType;
            }

            return databaseFile;
        }
    }
}
