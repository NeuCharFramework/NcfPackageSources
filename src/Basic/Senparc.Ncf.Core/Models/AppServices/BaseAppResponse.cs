using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models.AppServices
{
    /// <summary>
    /// AppService 响应详细基础模型（一般提供给序列化 JSON 使用）
    /// </summary>
    [Serializable]
    public class BaseAppResponse
    {
        public int StateCode { get;set;  }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public object Data { get; set; }

        public BaseAppResponse() { }

        public BaseAppResponse(int stateCode, bool success, string errorMessage, object data)
        {
            StateCode = stateCode;
            Success = success;
            ErrorMessage = errorMessage;
            Data = data;
        }
    }
}
