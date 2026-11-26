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
        /// <summary>Initializes an empty Swagger options object.</summary>
        public CustsomSwaggerOptions() { }
        /// <summary>Initializes Swagger options with project and API-version metadata.</summary>
        /// <param name="projectName">The display title of the API project.</param>
        /// <param name="apiVersions">The API document versions to expose.</param>
        public CustsomSwaggerOptions(string projectName, List<string> apiVersions)
        {
            ProjectName = projectName;
            ApiVersions = apiVersions;
        }

        /// <summary>
        /// Gets or sets whether Swagger endpoints and UI are enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the application path prefix used when constructing Swagger URLs.
        /// </summary>
        public string AppPath { get; set; }
        /// <summary>
        /// Gets or sets the project title displayed by Swagger UI.
        /// </summary>
        public string ProjectName { get; set; } = "My API";
        /// <summary>
        /// Gets or sets the API document versions exposed by Swagger.
        /// </summary>
        public List<string> ApiVersions { get; set; } = VersionHelper.GetApiVersions();
        /// <summary>
        /// Gets or sets the route prefix under which Swagger is served.
        /// </summary>
        public string RoutePrefix { get; set; } = "swagger";
        /// <summary>
        /// Gets or sets whether the embedded custom Swagger index page is used.
        /// </summary>
        public bool UseCustomIndex { get; set; }
        /// <summary>
        /// Gets or sets whether Swagger routes may be accessed anonymously.
        /// </summary>
        public bool AllowAnonymous { get; set; }
        /// <summary>
        /// Gets or sets whether the NCF administrator authentication scheme is used.
        /// </summary>
        public bool UseAdminAuth { get; set; }
        /// <summary>
        /// Gets or sets the custom username/password entries accepted by legacy Swagger authentication.
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
