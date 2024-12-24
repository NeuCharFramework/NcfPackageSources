using Senparc.CO2NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.AppServices
{
    public interface IAppResponse
    {
        int StateCode { get; set; }
        bool? Success { get; set; }
        string ErrorMessage { get; set; }
        object Data { get; set; }
        /// <summary>
        /// 请求临时ID（用于调取日志）
        /// </summary>
        string RequestTempId { get; }
    }

    /// <summary>
    /// AppService 响应详细基础模型（一般提供给序列化 JSON 使用）
    /// </summary>
    [Serializable]
    public class AppResponseBase : IAppResponse
    {
        public int StateCode { get; set; }
        public bool? Success { get; set; }
        public string ErrorMessage { get; set; }
        public object Data { get; set; }
        /// <summary>
        /// 请求临时ID（用于调取日志）
        /// </summary>
        public string RequestTempId { get; private set; }

        private string GenerateTempId(string domainCategoryForTempId)
        {
            var tempId = $"{SystemTime.NowTicks}-{Guid.NewGuid().ToString("n").Substring(0, 8)}";
            var domainCategory = (domainCategoryForTempId.IsNullOrEmpty() ? null : $"{domainCategoryForTempId}-");
            return $"RequestTempId-{domainCategory}{tempId}";
        }

        public AppResponseBase()
            : this(default(int), default(bool?), default(String), default(Object), null)
        { }

        public AppResponseBase(int stateCode, bool? success, string errorMessage, object data, string domainCategoryForTempId = null)
        {
            StateCode = stateCode;
            Success = success;
            ErrorMessage = errorMessage;
            Data = data;
            RequestTempId = GenerateTempId(domainCategoryForTempId);
        }

        public void ChangeRequestTempId(string newTempId)
        {
            RequestTempId = newTempId;
        }
    }

    /// <summary>
    /// AppService 响应详细基础模型（一般提供给序列化 JSON 使用）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class AppResponseBase<T> : AppResponseBase, IAppResponse
    {
        public AppResponseBase() : base() { }

        public new T Data { get => (T)base.Data; set => base.Data = value; }

        public AppResponseBase(int stateCode, bool success, string errorMessage, T data)
            : base(stateCode, success, errorMessage, data)
        {
        }
    }
}
