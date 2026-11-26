/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharFunctionService.cs
    File description: Related implementation.

    Created by: Senparc - 20260809

    Modified by: Senparc - 20260813
    Change description: v0.5.0 Updated module behavior and related support.

    Modified by: Senparc - 20260829
    Change description: v0.7.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Senparc.CO2NET.Helpers;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed record NeuCharFunctionDescriptor(
    string ModuleUid,
    string ModuleName,
    string ModuleVersion,
    bool ModuleAvailable,
    string FunctionKey,
    string Name,
    string Description,
    IReadOnlyList<FunctionParameterInfo> Parameters,
    NeuCharFunctionOutputDescriptor Output = null,
    string CatalogError = null,
    bool AllowGlobalPivot = false,
    IReadOnlyList<string> GlobalPivotRoleCodes = null,
    IReadOnlyList<string> GlobalPivotPermissionCodes = null,
    string MethodName = null);

public sealed record NeuCharFunctionOutputFieldDescriptor(
    string Path,
    string Label,
    string TypeName,
    bool IsArray,
    bool RequiresIndex);

public sealed record NeuCharFunctionOutputDescriptor(
    string TypeName,
    string DisplayName,
    bool IsArray,
    string ElementTypeName,
    IReadOnlyList<NeuCharFunctionOutputFieldDescriptor> Fields);

public sealed record NeuCharFunctionExecutionResult(
    bool Success,
    object Data,
    string ErrorMessage,
    string RequestTempId);

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public sealed class NeuCharFunctionService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly XncfModuleService _moduleService;

    public NeuCharFunctionService(IServiceProvider serviceProvider, XncfModuleService moduleService)
    {
        _serviceProvider = serviceProvider;
        _moduleService = moduleService;
    }

    /// <summary>
    /// Enumerates registered FunctionRender methods, optionally loads parameter metadata,
    /// and builds a sorted catalog including module availability and output descriptors.
    /// </summary>
    /// <param name="moduleUid">Optional module UID filter.</param>
    /// <param name="loadParameterOptions">Whether dynamic parameter options should be loaded.</param>
    /// <param name="cancellationToken">Token used to cancel catalog construction.</param>
    /// <returns>The sorted function catalog.</returns>
    public async Task<IReadOnlyList<NeuCharFunctionDescriptor>> GetCatalogAsync(
        string moduleUid = null,
        bool loadParameterOptions = true,
        CancellationToken cancellationToken = default)
    {
        var result = new List<NeuCharFunctionDescriptor>();
        var registers = XncfRegisterManager.RegisterList
            .Where(z => string.IsNullOrWhiteSpace(moduleUid) ||
                        string.Equals(z.Uid, moduleUid, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var register in registers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var module = await _moduleService.GetObjectAsync(z => z.Uid == register.Uid).ConfigureAwait(false);
            var available = module?.State == XncfModules_State.开放;

            if (!Senparc.Ncf.XncfBase.Register.FunctionRenderCollection.TryGetValue(register.GetType(), out var group))
            {
                continue;
            }

            foreach (var bag in group.Values.GroupBy(z => z.Key).Select(z => z.First()))
            {
                IReadOnlyList<FunctionParameterInfo> parameters;
                string catalogError = null;
                try
                {
                    parameters = await FunctionHelper.GetFunctionParameterInfoAsync(
                        _serviceProvider,
                        bag,
                        loadParameterOptions).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    catalogError = ex.Message;
                    try
                    {
                        parameters = await FunctionHelper.GetFunctionParameterInfoAsync(
                            _serviceProvider,
                            bag,
                            false).ConfigureAwait(false);
                    }
                    catch
                    {
                        parameters = Array.Empty<FunctionParameterInfo>();
                    }
                }

                result.Add(new NeuCharFunctionDescriptor(
                    register.Uid,
                    register.MenuName,
                    register.Version,
                    available,
                    bag.Key,
                    bag.FunctionRenderAttribute.Name,
                    bag.FunctionRenderAttribute.Description,
                    parameters,
                    BuildOutputDescriptor(bag.MethodInfo),
                    catalogError,
                    bag.FunctionRenderAttribute.AllowGlobalPivot,
                    ParseCodes(bag.FunctionRenderAttribute.GlobalPivotRoleCodes),
                    ParseCodes(bag.FunctionRenderAttribute.GlobalPivotPermissionCodes),
                    bag.MethodInfo.Name));
            }
        }

        return result
            .OrderBy(z => z.ModuleName)
            .ThenBy(z => z.Name)
            .ToList();
    }

    /// <summary>
    /// Resolves and invokes one registered FunctionRender method from a JSON request,
    /// enforcing module availability, required parameters, request-size limits, and
    /// the single-request-object invocation contract.
    /// </summary>
    /// <param name="moduleUid">The registered module UID.</param>
    /// <param name="functionKeyOrName">The function key, display name, or method name.</param>
    /// <param name="parametersJson">The JSON request object.</param>
    /// <param name="cancellationToken">Token used to cancel execution.</param>
    /// <returns>A normalized function execution result; invocation failures are returned as failed results.</returns>
    public async Task<NeuCharFunctionExecutionResult> ExecuteAsync(
        string moduleUid,
        string functionKeyOrName,
        string parametersJson,
        CancellationToken cancellationToken = default)
    {
        if (parametersJson?.Length > 1_000_000)
        {
            return Failure("Function 参数不能超过 1000000 个字符。");
        }

        var register = XncfRegisterManager.RegisterList.FirstOrDefault(z =>
            string.Equals(z.Uid, moduleUid, StringComparison.OrdinalIgnoreCase));
        if (register == null)
        {
            return Failure("模块未注册或程序集未加载。");
        }

        var module = await _moduleService.GetObjectAsync(z => z.Uid == register.Uid).ConfigureAwait(false);
        if (module == null)
        {
            return Failure("模块尚未安装。");
        }

        if (module.State != XncfModules_State.开放)
        {
            return Failure($"模块当前状态为 {module.State}，Function 已被禁用。");
        }

        if (!Senparc.Ncf.XncfBase.Register.FunctionRenderCollection.TryGetValue(register.GetType(), out var group))
        {
            return Failure("模块没有已注册的 Function。");
        }

        var bag = group.Values.FirstOrDefault(z =>
            string.Equals(z.Key, functionKeyOrName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(z.FunctionRenderAttribute.Name, functionKeyOrName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(z.MethodInfo?.Name, functionKeyOrName, StringComparison.OrdinalIgnoreCase));
        if (bag.MethodInfo == null)
        {
            return Failure("Function 不存在或版本升级后已被移除。");
        }

        var parameterInfos = await FunctionHelper.GetFunctionParameterInfoAsync(_serviceProvider, bag, true)
            .ConfigureAwait(false);
        var validationError = ValidateRequiredParameters(parameterInfos, parametersJson);
        if (validationError != null)
        {
            return Failure(validationError);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var methodParameters = bag.MethodInfo.GetParameters();
            object[] arguments = null;
            if (methodParameters.Length == 1)
            {
                var parameterType = methodParameters[0].ParameterType;
                var normalizedJson = FunctionRequestParameterNormalizer.NormalizeJson(
                    string.IsNullOrWhiteSpace(parametersJson) ? "{}" : parametersJson,
                    parameterType);
                var request = SerializerHelper.GetObject(normalizedJson, parameterType) as IAppRequest;
                if (request == null)
                {
                    return Failure("Function 参数无法转换为请求对象。");
                }
                arguments = new object[] { request };
            }
            else if (methodParameters.Length > 1)
            {
                return Failure("当前仅支持零参数或单请求对象参数的 Function。");
            }

            var target = _serviceProvider.GetService(bag.MethodInfo.DeclaringType);
            if (target == null)
            {
                return Failure("Function 服务未注册，无法执行。");
            }

            if (bag.MethodInfo.Invoke(target, arguments) is not Task task)
            {
                return Failure("Function 必须返回 Task<IAppResponse>。");
            }

            await task.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            var resultProperty = task.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
            var response = resultProperty?.GetValue(task) as IAppResponse;
            return response == null
                ? Failure("Function 未返回有效的 IAppResponse。")
                : new NeuCharFunctionExecutionResult(
                    response.Success == true,
                    response.Data,
                    response.ErrorMessage,
                    response.RequestTempId);
        }
        catch (TargetInvocationException ex)
        {
            return Failure(ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            return Failure(ex.Message);
        }
    }

    /// <summary>
    /// Validates that every required function parameter exists and is non-empty
    /// in a JSON object.
    /// </summary>
    /// <param name="parameterInfos">The function parameter metadata.</param>
    /// <param name="parametersJson">The JSON request payload.</param>
    /// <returns><see langword="null"/> when valid; otherwise a localized validation message.</returns>
    public static string ValidateRequiredParameters(
        IReadOnlyList<FunctionParameterInfo> parameterInfos,
        string parametersJson)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(string.IsNullOrWhiteSpace(parametersJson) ? "{}" : parametersJson);
        }
        catch (JsonException)
        {
            return "参数不是有效的 JSON 对象。";
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return "参数必须是 JSON 对象。";
            }

            foreach (var parameter in parameterInfos.Where(z => z.IsRequired))
            {
                if (!TryGetPropertyIgnoreCase(document.RootElement, parameter.Name, out var value) || IsEmpty(value))
                {
                    return $"必填参数“{parameter.Title ?? parameter.Name}”尚未提供。";
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Retrieves a JSON object property using case-insensitive matching.
    /// </summary>
    /// <param name="element">The JSON object.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">Receives the property value.</param>
    /// <returns><see langword="true"/> when a matching property exists.</returns>
    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Determines whether a JSON value represents an absent or empty required input.
    /// </summary>
    /// <param name="value">The JSON value to inspect.</param>
    /// <returns><see langword="true"/> for null, undefined, blank strings, or empty arrays.</returns>
    private static bool IsEmpty(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => true,
        JsonValueKind.String => string.IsNullOrWhiteSpace(value.GetString()),
        JsonValueKind.Array => value.GetArrayLength() == 0,
        _ => false
    };

    /// <summary>
    /// Reflects a FunctionRender return type into a bounded, navigable output-field descriptor.
    /// </summary>
    /// <param name="methodInfo">The function method to inspect.</param>
    /// <returns>The normalized output descriptor.</returns>
    public static NeuCharFunctionOutputDescriptor BuildOutputDescriptor(MethodInfo methodInfo)
    {
        var dataType = GetResponseDataType(methodInfo?.ReturnType) ?? typeof(object);
        var isArray = TryGetCollectionElementType(dataType, out var elementType);
        var valueType = isArray ? elementType : dataType;
        var fields = new List<NeuCharFunctionOutputFieldDescriptor>
        {
            new(
                "$",
                isArray ? "完整列表" : "完整输出",
                NormalizeValueType(valueType),
                isArray,
                false)
        };
        AddOutputFields(fields, valueType, "$", string.Empty, isArray, 0, new HashSet<Type>());
        return new NeuCharFunctionOutputDescriptor(
            NormalizeValueType(valueType),
            GetFriendlyTypeName(dataType),
            isArray,
            isArray ? NormalizeValueType(elementType) : null,
            fields);
    }

    /// <summary>
    /// Maps a CLR type to the output schema vocabulary used by the Admin UI.
    /// </summary>
    /// <param name="type">The CLR type.</param>
    /// <returns>A normalized string, boolean, datetime, number, object, or any type name.</returns>
    public static string NormalizeValueType(Type type)
    {
        type ??= typeof(object);
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(string) || type == typeof(char) || type == typeof(Guid) || type == typeof(Uri))
        {
            return "string";
        }
        if (type == typeof(bool))
        {
            return "boolean";
        }
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan))
        {
            return "datetime";
        }
        if (type.IsEnum)
        {
            return "string";
        }
        if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) ||
            type == typeof(ushort) || type == typeof(int) || type == typeof(uint) ||
            type == typeof(long) || type == typeof(ulong) || type == typeof(float) ||
            type == typeof(double) || type == typeof(decimal))
        {
            return "number";
        }
        return type == typeof(object) ? "any" : "object";
    }

    /// <summary>
    /// Extracts the data type from Task and AppResponseBase return wrappers.
    /// </summary>
    /// <param name="returnType">The reflected method return type.</param>
    /// <returns>The response data type, or <see langword="null"/> when unrecognized.</returns>
    private static Type GetResponseDataType(Type returnType)
    {
        if (returnType == null)
        {
            return null;
        }
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            returnType = returnType.GetGenericArguments()[0];
        }
        for (var current = returnType; current != null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AppResponseBase<>))
            {
                return current.GetGenericArguments()[0];
            }
        }
        return typeof(IAppResponse).IsAssignableFrom(returnType) ? typeof(object) : null;
    }

    /// <summary>
    /// Recursively adds readable object properties to an output descriptor up to
    /// the depth and property-count safety bounds.
    /// </summary>
    /// <param name="fields">The output field collection.</param>
    /// <param name="type">The current CLR type.</param>
    /// <param name="path">The current output path.</param>
    /// <param name="labelPrefix">The current display-label prefix.</param>
    /// <param name="parentRequiresIndex">Whether an ancestor array requires indexing.</param>
    /// <param name="depth">The current recursion depth.</param>
    /// <param name="ancestors">Types already visited on the current branch.</param>
    private static void AddOutputFields(
        ICollection<NeuCharFunctionOutputFieldDescriptor> fields,
        Type type,
        string path,
        string labelPrefix,
        bool parentRequiresIndex,
        int depth,
        ISet<Type> ancestors)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (depth >= 3 || IsSimpleType(type) || !ancestors.Add(type))
        {
            return;
        }
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(z => z.CanRead && z.GetIndexParameters().Length == 0)
                     .Take(80))
        {
            var propertyIsArray = TryGetCollectionElementType(property.PropertyType, out var elementType);
            var propertyValueType = propertyIsArray ? elementType : property.PropertyType;
            var propertyPath = $"{path}.{property.Name}";
            var label = string.IsNullOrWhiteSpace(labelPrefix)
                ? property.Name
                : $"{labelPrefix}.{property.Name}";
            fields.Add(new NeuCharFunctionOutputFieldDescriptor(
                propertyPath,
                label,
                NormalizeValueType(propertyValueType),
                propertyIsArray,
                parentRequiresIndex));
            AddOutputFields(
                fields,
                propertyValueType,
                propertyPath,
                label,
                parentRequiresIndex || propertyIsArray,
                depth + 1,
                new HashSet<Type>(ancestors));
        }
    }

    /// <summary>
    /// Detects arrays and generic enumerable types and returns their element type.
    /// </summary>
    /// <param name="type">The CLR type to inspect.</param>
    /// <param name="elementType">Receives the collection element type or the original type.</param>
    /// <returns><see langword="true"/> when the type is a collection.</returns>
    private static bool TryGetCollectionElementType(Type type, out Type elementType)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type != typeof(string) && type.IsArray)
        {
            elementType = type.GetElementType() ?? typeof(object);
            return true;
        }
        var enumerable = type == typeof(string)
            ? null
            : type.GetInterfaces()
                .Concat(new[] { type })
                .FirstOrDefault(z => z.IsGenericType && z.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (enumerable != null)
        {
            elementType = enumerable.GetGenericArguments()[0];
            return true;
        }
        elementType = type;
        return false;
    }

    /// <summary>
    /// Determines whether a type does not require recursive property expansion.
    /// </summary>
    /// <param name="type">The CLR type.</param>
    /// <returns><see langword="true"/> for primitive/schema scalar types.</returns>
    private static bool IsSimpleType(Type type) =>
        NormalizeValueType(type) is not ("object" or "any");

    /// <summary>
    /// Produces a compact display name for a scalar or collection type.
    /// </summary>
    /// <param name="type">The CLR type.</param>
    /// <returns>The friendly type name.</returns>
    private static string GetFriendlyTypeName(Type type)
    {
        if (TryGetCollectionElementType(type, out var elementType))
        {
            return $"{elementType.Name}[]";
        }
        return (Nullable.GetUnderlyingType(type) ?? type).Name;
    }

    /// <summary>
    /// Creates a failed function execution result with no data or request temp ID.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A failed execution result.</returns>
    private static NeuCharFunctionExecutionResult Failure(string message) =>
        new(false, null, message, null);

    /// <summary>
    /// Splits comma-separated role/permission codes, trims them, and removes duplicates.
    /// </summary>
    /// <param name="codes">The raw comma-separated code string.</param>
    /// <returns>Distinct normalized codes.</returns>
    private static IReadOnlyList<string> ParseCodes(string codes)
    {
        return string.IsNullOrWhiteSpace(codes)
            ? Array.Empty<string>()
            : codes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }
}
