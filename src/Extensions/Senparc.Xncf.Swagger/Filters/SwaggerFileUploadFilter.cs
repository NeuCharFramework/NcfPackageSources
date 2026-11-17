/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: SwaggerFileUploadFilter.cs

    File description: SwaggerFileUploadFilter related implementation

    

    

    Created by: Senparc - 20210614

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi; //Legacy comment translated to English.
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Senparc.Xncf.Swagger.Filters
{
    public class SwaggerFileUploadFilter : IOperationFilter
    {
        private static readonly string[] FormFilePropertyNames = typeof(IFormFile).GetTypeInfo().DeclaredProperties.Select(x => x.Name).ToArray();

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (!context.ApiDescription.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
                !context.ApiDescription.HttpMethod.Equals("PUT", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var fileParameters = context.ApiDescription.ActionDescriptor.Parameters
                .Where(n => n.ParameterType == typeof(IFormFile) ||
                            n.ParameterType == typeof(IEnumerable<IFormFile>) ||
                            n.ParameterType == typeof(IFormFileCollection))
                .ToList();

            if (fileParameters.Count == 0) return;

            if (operation.RequestBody?.Content == null) return;

            if (operation.RequestBody.Content.TryGetValue("multipart/form-data", out var mediaType))
            {
                var schema = mediaType.Schema;
                if (schema == null) return;

                //Legacy comment translated to English.

                //Legacy comment translated to English.
                foreach (var propName in FormFilePropertyNames)
                {
                    if (schema.Properties.ContainsKey(propName))
                    {
                        schema.Properties.Remove(propName);
                    }
                }

                //Legacy comment translated to English.
                foreach (var fileParam in fileParameters)
                {
                    var parameterName = fileParam.Name ?? "file";

                    var fileSchema = new OpenApiSchema()
                    {
                        Title = "上传文件",
                        Type = JsonSchemaType.String,
                        Format = "binary",
                        Description = $"选择文件: {parameterName}"
                    };

                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    schema.Properties[parameterName] = fileSchema;

                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    if (schema.Required != null)
                    {
                        if (!schema.Required.Contains(parameterName))
                        {
                            schema.Required.Add(parameterName);
                        }
                    }
                }
            }
        }
    }
}