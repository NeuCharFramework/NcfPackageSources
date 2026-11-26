/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SwaggerDefaultValueFilter.cs
    File description: SwaggerDefaultValueFilter related implementation

    Created by: Senparc - 20210614

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.12 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Collections.Generic;

namespace Senparc.Xncf.Swagger.Filters
{
    /// <summary>
    /// Copies MVC parameter descriptions and required-route metadata into generated OpenAPI operations.
    /// </summary>
    public class SwaggerDefaultValueFilter : IOperationFilter
    {
        /// <summary>
        /// Copies MVC parameter descriptions and route-required metadata into the OpenAPI operation.
        /// </summary>
        /// <param name="operation">The OpenAPI operation being generated.</param>
        /// <param name="context">The API description and metadata context.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null || operation.Parameters.Count == 0)
            {
                return;
            }

            for (int i = 0; i < operation.Parameters.Count; i++)
            {
                var parameter = operation.Parameters[i];

                var description = context.ApiDescription.ParameterDescriptions
                                    .FirstOrDefault(p => p.Name == parameter.Name);

                if (description == null) continue;

                var newDescription = parameter.Description ?? description.ModelMetadata?.Description;
                var newRequired = parameter.Required;

                if (description.RouteInfo != null)
                {
                    newRequired |= !description.RouteInfo.IsOptional;
                }

                if (newDescription != parameter.Description || newRequired != parameter.Required)
                {
                    operation.Parameters[i] = new OpenApiParameter
                    {
                        Name = parameter.Name,
                        In = parameter.In,
                        Schema = parameter.Schema,
                        Style = parameter.Style,
                        Explode = parameter.Explode,
                        Example = parameter.Example,
                        Examples = parameter.Examples,
                        Content = parameter.Content,
                        Deprecated = parameter.Deprecated,
                        Extensions = parameter.Extensions,
                        Description = newDescription,
                        Required = newRequired
                    };
                }
            }
        }
    }
}
