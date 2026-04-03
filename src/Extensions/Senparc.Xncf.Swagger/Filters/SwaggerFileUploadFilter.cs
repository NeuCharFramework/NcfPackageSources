using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi; // where the core model is
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

                // --- Note: Since Properties is read-only, we operate the collection directly ---

                // 1. Cleanup: Remove IFormFile default properties
                foreach (var propName in FormFilePropertyNames)
                {
                    if (schema.Properties.ContainsKey(propName))
                    {
                        schema.Properties.Remove(propName);
                    }
                }

                // 2. Inject new attributes
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

                    // Use indexer: overwrite if exists, add if not
                    // Because properties are read-only, you cannot use schema.Properties = ...
                    schema.Properties[parameterName] = fileSchema;

                    // 3. Process required items (Required is also a read-only ISet<string>)
                    // Usually the framework will pre-initialize this collection. If it is not initialized (that is, it is null),
                    // In this read-only design, a constructor or initialization method is usually provided.
                    // But in most cases, schema.Required has already been instantiated at this time.
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