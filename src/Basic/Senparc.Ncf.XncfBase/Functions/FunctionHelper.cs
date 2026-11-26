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

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

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
    /// Provides reflection and filesystem helpers used to describe and prepare rendered functions.
    /// </summary>
    public class FunctionHelper
    {
        /// <summary>
        /// Appends a timestamped function log line to a string builder.
        /// </summary>
        /// <param name="sb">The log builder.</param>
        /// <param name="msg">The message to append.</param>
        public static void RecordLog(StringBuilder sb, string msg)
        {
            sb.AppendLine($"[{SystemTime.Now.ToString()}]\t{msg}");
        }

        /// <summary>
        /// Reflects a function request type into parameter metadata, optionally loading
        /// dynamic selection data from the service provider and resolving localized descriptions.
        /// </summary>
        /// <param name="serviceProvider">The provider used by request data-loading hooks.</param>
        /// <param name="functionRenderBag">The function metadata and request type.</param>
        /// <param name="tryLoadData">Whether to invoke the request's data-loading hook.</param>
        /// <returns>The reflected function parameter metadata.</returns>
        /// <exception cref="Exception">Propagates failures raised while loading dynamic request data.</exception>
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

        /// <summary>
        /// Determines whether a property has either System.Text.Json or Newtonsoft.Json ignore metadata.
        /// </summary>
        /// <param name="propertyInfo">The property to inspect.</param>
        /// <returns><see langword="true"/> when the property should be excluded from function parameters.</returns>
        private static bool ShouldIgnoreProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo.GetCustomAttribute<JsonIgnoreAttribute>() != null)
            {
                return true;
            }

            return propertyInfo.CustomAttributes.Any(z => z.AttributeType.FullName == "Newtonsoft.Json.JsonIgnoreAttribute");
        }

        /// <summary>
        /// Determines whether a property is a Boolean or nullable Boolean.
        /// </summary>
        /// <param name="prop">The property to inspect.</param>
        /// <returns><see langword="true"/> when the property represents Boolean data.</returns>
        private static bool IsBooleanProperty(PropertyInfo prop)
        {
            var t = prop.PropertyType;
            return t == typeof(bool) || Nullable.GetUnderlyingType(t) == typeof(bool);
        }

        /// <summary>
        /// Determines whether a type is a supported collection of Function file references.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns><see langword="true"/> for arrays/lists/read-only lists/enumerables of file references.</returns>
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
        /// Searches upward for a solution and returns selectable standard XNCF project folders.
        /// Additional glob patterns can extend the discovered project set.
        /// </summary>
        /// <param name="mustHaveXncfModule">Whether to return an N/A option when no project is found.</param>
        /// <param name="rootDir">Optional directory from which the solution search begins.</param>
        /// <param name="additionalProjects">Additional directory patterns to include.</param>
        /// <returns>Selection entries representing discovered XNCF projects.</returns>
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
        /// Resolves the database entity file name for a project and optional database suffix.
        /// </summary>
        /// <param name="projectPath">The project directory.</param>
        /// <param name="dbType">Optional database provider suffix.</param>
        /// <returns>The entity file name without its <c>.cs</c> extension.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when no matching entity file exists.</exception>
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
