using Senparc.Ncf.Core.AppServices;
using System;
using System.Collections.Generic;
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
        }

        public string Key { get; set; }
        public MethodInfo MethodInfo { get; set; }
        public FunctionRenderAttribute FunctionRenderAttribute { get; set; }

    }
}
