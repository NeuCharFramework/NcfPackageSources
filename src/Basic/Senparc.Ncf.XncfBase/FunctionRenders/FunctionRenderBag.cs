using Senparc.Ncf.Core.AppServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Senparc.Ncf.XncfBase.FunctionRenders
{
    public struct FunctionRenderBag
    {
        public FunctionRenderBag(string key, MethodInfo methodInfo, FunctionRenderAttribute functionRenderAttribute)
        {
            MethodInfo = methodInfo;
            FunctionRenderAttribute = functionRenderAttribute;
            Key = key;
            FunctionParameterType = MethodInfo?.GetParameters().FirstOrDefault()?.GetType() ?? typeof(FunctionAppRequestBase);
        }

        public string Key { get; set; }
        public MethodInfo MethodInfo { get; set; }

        public Type FunctionParameterType { get; set; }

        public FunctionRenderAttribute FunctionRenderAttribute { get; set; }

    }
}
