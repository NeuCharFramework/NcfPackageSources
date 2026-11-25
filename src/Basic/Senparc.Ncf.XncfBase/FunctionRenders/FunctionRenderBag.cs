/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FunctionRenderBag.cs
    File description: FunctionRenderBag related implementation

    Created by: Senparc - 20211013

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Senparc.Ncf.Core.AppServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Senparc.Ncf.XncfBase.FunctionRenders
{
    /// <summary>
    /// Stores the reflection metadata required to invoke and render one registered function.
    /// </summary>
    public struct FunctionRenderBag
    {
        /// <summary>
        /// Initializes a function-render metadata bag and derives its request parameter type.
        /// </summary>
        /// <param name="key">The registration key used to identify the function.</param>
        /// <param name="methodInfo">The reflected method to invoke.</param>
        /// <param name="functionRenderAttribute">The function-render configuration attribute.</param>
        public FunctionRenderBag(string key, MethodInfo methodInfo, FunctionRenderAttribute functionRenderAttribute)
        {
            MethodInfo = methodInfo;
            FunctionRenderAttribute = functionRenderAttribute;
            Key = key;
            FunctionParameterType = MethodInfo?.GetParameters().FirstOrDefault()?.ParameterType ?? typeof(FunctionAppRequestBase);
        }

        public string Key { get; set; }
        public MethodInfo MethodInfo { get; set; }

        public Type FunctionParameterType { get; set; }

        public FunctionRenderAttribute FunctionRenderAttribute { get; set; }

    }
}
