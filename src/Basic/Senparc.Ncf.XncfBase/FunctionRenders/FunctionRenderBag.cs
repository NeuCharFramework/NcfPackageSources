/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FunctionRenderBag.cs
    文件功能描述：FunctionRenderBag 相关实现
    
    
    创建标识：Senparc - 20211013
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
            FunctionParameterType = MethodInfo?.GetParameters().FirstOrDefault()?.ParameterType ?? typeof(FunctionAppRequestBase);
        }

        public string Key { get; set; }
        public MethodInfo MethodInfo { get; set; }

        public Type FunctionParameterType { get; set; }

        public FunctionRenderAttribute FunctionRenderAttribute { get; set; }

    }
}
