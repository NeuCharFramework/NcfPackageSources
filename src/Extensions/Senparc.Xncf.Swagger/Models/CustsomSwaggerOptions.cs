/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: CustsomSwaggerOptions.cs
    File description: CustsomSwaggerOptions related implementation

    Created by: Senparc - 20210614

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.12 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

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
        /// whether enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        public string AppPath { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string ProjectName { get; set; } = "My API";
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public List<string> ApiVersions { get; set; } = VersionHelper.GetApiVersions();
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string RoutePrefix { get; set; } = "swagger";
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool UseCustomIndex { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool AllowAnonymous { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool UseAdminAuth { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
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
