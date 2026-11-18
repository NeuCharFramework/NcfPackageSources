/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FunctionHelper.cs
    File description: FunctionHelper related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260822
    Change description: v0.26.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.28.1 add bulk file upload functionality to Sandbox Lab

----------------------------------------------------------------*/

using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Localization;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions.Parameters;
using Senparc.Ncf.XncfBase.Functions.Files;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.Functions
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class FunctionHelper
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="msg"></param>
        public static void RecordLog(StringBuilder sb, string msg)
        {
            sb.AppendLine($"[{SystemTime.Now.ToString()}]\t{msg}");
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="functionRenderBag">FunctionRenderBag</param>
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static async Task<List<FunctionParameterInfo>> GetFunctionParameterInfoAsync(IServiceProvider serviceProvider, FunctionRenderBag functionRenderBag, bool tryLoadData = true)
        {
            var functionParameterType = functionRenderBag.FunctionParameterType;
            var registerType = functionRenderBag.FunctionRenderAttribute.RegisterType;

            IAppRequest paraObj = null;

            if (ReflectionHelper.HasParameterlessConstructor(functionParameterType))
            {
                //Legacy comment translated to English.
                paraObj = functionParameterType.Assembly.CreateInstance(functionParameterType.FullName) as IAppRequest;//Legacy comment translated to English.

                if (tryLoadData && paraObj is FunctionAppRequestBase functionRequestPara)
                {
                    try
                    {
                        await functionRequestPara.LoadData(serviceProvider);//Legacy comment translated to English.
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
                if (ShouldIgnoreProperty(prop))
                {
                    continue;
                }

                SelectionList selectionList = null;
                var filterable = false;
                var allowCreate = false;
                parameterType = ParameterType.Text;//Legacy comment translated to English.

                var functionParameterUi = prop.GetCustomAttribute<FunctionParameterUiAttribute>();
                if (functionParameterUi != null)
                {
                    parameterType = functionParameterUi.ParameterType;
                    filterable = functionParameterUi.Filterable;
                    allowCreate = functionParameterUi.AllowCreate;

                    if (!functionParameterUi.SelectionListPropertyName.IsNullOrEmpty() && paraObj != null)
                    {
                        var selectionProp = functionParameterType.GetProperty(functionParameterUi.SelectionListPropertyName);
                        if (selectionProp?.PropertyType == typeof(SelectionList))
                        {
                            selectionList = selectionProp.GetValue(paraObj, null) as SelectionList;
                        }
                    }
                }
                else if (prop.PropertyType == typeof(SelectionList) && paraObj != null)
                {
                    var selections = prop.GetValue(paraObj, null) as SelectionList;
                    switch (selections?.SelectionType)
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
                else if (IsBooleanProperty(prop))
                {
                    parameterType = ParameterType.CheckBox;
                }

                var allowMultiple = false;
                var maxFiles = 0;
                var accept = string.Empty;
                if (parameterType == ParameterType.File)
                {
                    allowMultiple = functionParameterUi?.AllowMultiple == true ||
                                    IsFileCollection(prop.PropertyType);
                    maxFiles = functionParameterUi?.MaxFiles ?? 0;
                    accept = functionParameterUi?.Accept ?? string.Empty;
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
                    //Legacy comment translated to English.
                    var descriptionAttrArr = descriptionAttr.Description.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                    title = descriptionAttrArr[0];
                    if (descriptionAttrArr.Length > 1)
                    {
                        description = descriptionAttrArr[1];
                    }

                    title = FunctionRenderTextResolver.Resolve(registerType, title);
                    description = FunctionRenderTextResolver.Resolve(registerType, description);
                }

                if (maxLengthAttr != null)
                {
                    maxLength = maxLengthAttr.Length;
                }

                var systemType = prop.PropertyType.Name;

                object value = null;
                if (paraObj != null)
                {
                    try
                    {
                        value = prop.GetValue(paraObj);
                    }
                    catch (Exception ex)
                    {
                        SenparcTrace.BaseExceptionLog(ex);
                    }
                }

                if (selectionList != null && registerType != null)
                {
                    var selectedValues = selectionList.SelectedValues;
                    selectionList = new SelectionList(
                        selectionList.SelectionType,
                        selectionList.Items.Select(item => new SelectionItem(
                            item.Value,
                            FunctionRenderTextResolver.Resolve(registerType, item.Text),
                            FunctionRenderTextResolver.Resolve(registerType, item.Note),
                            item.DefaultSelected)
                        {
                            BindData = item.BindData
                        }).ToList())
                    {
                        SelectedValues = selectedValues
                    };
                }

                var functionParamInfo = new FunctionParameterInfo(name, title, description, isRequired, systemType, parameterType,
                                                selectionList ?? new SelectionList(SelectionType.Unknown), value, maxLength,
                                                filterable, allowCreate);
                functionParamInfo.AllowMultiple = allowMultiple;
                functionParamInfo.MaxFiles = maxFiles;
                functionParamInfo.Accept = accept;
                result.Add(functionParamInfo);
            }
            return result;
        }

        private static bool ShouldIgnoreProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo.GetCustomAttribute<JsonIgnoreAttribute>() != null)
            {
                return true;
            }

            return propertyInfo.CustomAttributes.Any(z => z.AttributeType.FullName == "Newtonsoft.Json.JsonIgnoreAttribute");
        }

        private static bool IsBooleanProperty(PropertyInfo prop)
        {
            var t = prop.PropertyType;
            return t == typeof(bool) || Nullable.GetUnderlyingType(t) == typeof(bool);
        }

        private static bool IsFileCollection(Type type)
        {
            if (type == typeof(FunctionFileReference[]))
            {
                return true;
            }

            return type.IsGenericType &&
                   (type.GetGenericTypeDefinition() == typeof(List<>) ||
                    type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>) ||
                    type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) &&
                   type.GetGenericArguments()[0] == typeof(FunctionFileReference);
        }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
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
                //Legacy comment translated to English.

                //Legacy comment translated to English.

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
                    //Legacy comment translated to English.
                    var registerFilePath = Path.Combine(projectFolder, "Register.cs");
                    if (!File.Exists(registerFilePath))
                    {
                        continue;//Legacy comment translated to English.
                    }

                    //Legacy comment translated to English.

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
        /// get xxxSenparcEntities.cs database file
        /// </summary>
        /// <param name="projectPath"></param>
        /// <param name="dbType">databasetype</param>
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
