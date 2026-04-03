using Senparc.Xncf.Swagger.Utils;

using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

using System;
using System.Collections.Generic;

namespace Senparc.Xncf.Swagger.Models
{
    public class CustsomSwaggerOptions
    {
        public CustsomSwaggerOptions() { }
        public CustsomSwaggerOptions(string projectName, List<string> apiVersions)
        {
            ProjectName = projectName;
            ApiVersions = apiVersions;
        }

        /// <summary>
        /// Whether to enable
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Project release path Directory name of the sub-application (virtual site)
        /// When publishing directly as a site, keep the default value
        /// </summary>
        public string AppPath { get; set; }
        /// <summary>
        ///project name
        /// </summary>
        public string ProjectName { get; set; } = "My API";
        /// <summary>
        /// Interface document display version
        /// </summary>
        public List<string> ApiVersions { get; set; } = VersionHelper.GetApiVersions();
        /// <summary>
        ///Interface document access route prefix
        /// </summary>
        public string RoutePrefix { get; set; } = "swagger";
        /// <summary>
        /// Use custom homepage
        /// </summary>
        public bool UseCustomIndex { get; set; }
        /// <summary>
        ///Allow anonymous access
        /// </summary>
        public bool AllowAnonymous { get; set; }
        /// <summary>
        /// Use the Admin module of the main project to authenticate and log in
        /// </summary>
        public bool UseAdminAuth { get; set; }
        /// <summary>
        /// swagger login account, if not specified, it will not be enabled.
        /// </summary>
        public List<CustomSwaggerAuth> CustomAuthList { get; set; }
        /// <summary>
        /// UseSwagger Hook
        /// </summary>
        public Action<SwaggerOptions> UseSwaggerAction { get; set; }
        /// <summary>
        /// UseSwaggerUI Hook
        /// </summary>
        public Action<SwaggerUIOptions> UseSwaggerUIAction { get; set; }
        /// <summary>
        /// AddSwaggerGen Hook
        /// </summary>
        public Action<SwaggerGenOptions> AddSwaggerGenAction { get; set; }
    }
}
