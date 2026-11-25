/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AppResponseBase.cs
    File description: AppResponseBase related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Senparc.CO2NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.AppServices
{
    /// <summary>
    /// Defines the common fields returned by an application service response.
    /// </summary>
    public interface IAppResponse
    {
        /// <summary>Gets or sets the application-specific response state code.</summary>
        int StateCode { get; set; }
        /// <summary>Gets or sets whether the operation succeeded; <see langword="null"/> means unspecified.</summary>
        bool? Success { get; set; }
        /// <summary>Gets or sets the human-readable error message, when any.</summary>
        string ErrorMessage { get; set; }
        /// <summary>Gets or sets the response payload.</summary>
        object Data { get; set; }
        /// <summary>
        /// Gets the temporary request identifier used to retrieve correlated logs.
        /// </summary>
        string RequestTempId { get; }
    }

    /// <summary>
    /// Represents the non-generic response envelope returned by application services.
    /// </summary>
    [Serializable]
    public class AppResponseBase : IAppResponse
    {
        /// <summary>
        /// Gets or sets the application-specific response state code.
        /// </summary>
        public int StateCode { get; set; }
        /// <summary>Gets or sets whether the operation succeeded; <see langword="null"/> means unspecified.</summary>
        public bool? Success { get; set; }
        /// <summary>Gets or sets the human-readable error message, when any.</summary>
        public string ErrorMessage { get; set; }
        /// <summary>Gets or sets the response payload.</summary>
        public object Data { get; set; }
        /// <summary>
        /// Gets the temporary request identifier used to retrieve correlated logs.
        /// </summary>
        public string RequestTempId { get; private set; }

        /// <summary>
        /// Generates a request identifier containing an optional domain category.
        /// </summary>
        /// <param name="domainCategoryForTempId">An optional category prefix used to distinguish request domains.</param>
        /// <returns>A unique temporary request identifier.</returns>
        private string GenerateTempId(string domainCategoryForTempId)
        {
            var tempId = $"{SystemTime.NowTicks}-{Guid.NewGuid().ToString("n").Substring(0, 8)}";
            var domainCategory = (domainCategoryForTempId.IsNullOrEmpty() ? null : $"{domainCategoryForTempId}-");
            return $"RequestTempId-{domainCategory}{tempId}";
        }

        /// <summary>
        /// Initializes an empty response envelope with a generated request identifier.
        /// </summary>
        public AppResponseBase()
            : this(default(int), default(bool?), default(String), default(Object), null)
        { }

        /// <summary>
        /// Initializes a response envelope with its status, success flag, error text, payload,
        /// and optional log-correlation category.
        /// </summary>
        /// <param name="stateCode">The application-specific response state code.</param>
        /// <param name="success">Whether the operation succeeded.</param>
        /// <param name="errorMessage">The error message associated with a failed operation.</param>
        /// <param name="data">The response payload.</param>
        /// <param name="domainCategoryForTempId">An optional category prefix for the generated request identifier.</param>
        public AppResponseBase(int stateCode, bool? success, string errorMessage, object data, string domainCategoryForTempId = null)
        {
            StateCode = stateCode;
            Success = success;
            ErrorMessage = errorMessage;
            Data = data;
            RequestTempId = GenerateTempId(domainCategoryForTempId);
        }

        /// <summary>
        /// Replaces the temporary request identifier used for log correlation.
        /// </summary>
        /// <param name="newTempId">The identifier to expose to callers.</param>
        public void ChangeRequestTempId(string newTempId)
        {
            RequestTempId = newTempId;
        }
    }

    /// <summary>
    /// Represents a response envelope whose payload is strongly typed as <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The payload type carried by the response.</typeparam>
    [Serializable]
    public class AppResponseBase<T> : AppResponseBase, IAppResponse
    {
        /// <summary>Initializes an empty typed response envelope.</summary>
        public AppResponseBase() : base() { }

        /// <summary>Gets or sets the strongly typed response payload.</summary>
        public new T Data { get => (T)base.Data; set => base.Data = value; }

        /// <summary>
        /// Initializes a typed response envelope with its status and payload.
        /// </summary>
        /// <param name="stateCode">The application-specific response state code.</param>
        /// <param name="success">Whether the operation succeeded.</param>
        /// <param name="errorMessage">The error message associated with a failed operation.</param>
        /// <param name="data">The strongly typed response payload.</param>
        public AppResponseBase(int stateCode, bool success, string errorMessage, T data)
            : base(stateCode, success, errorMessage, data)
        {
        }
    }
}
