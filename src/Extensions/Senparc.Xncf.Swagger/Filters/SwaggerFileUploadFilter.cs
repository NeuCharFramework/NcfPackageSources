using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi; // 核心模型所在
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

                // --- 注意：由于 Properties 是只读的，我们直接操作集合 ---

                // 1. 清理：移除 IFormFile 默认属性
                foreach (var propName in FormFilePropertyNames)
                {
                    if (schema.Properties.ContainsKey(propName))
                    {
                        schema.Properties.Remove(propName);
                    }
                }

                // 2. 注入新属性
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

                    // 使用索引器：如果存在则覆盖，不存在则添加
                    // 因为属性是只读的，所以不能用 schema.Properties = ...
                    schema.Properties[parameterName] = fileSchema;

                    // 3. 处理必填项 (Required 也是只读的 ISet<string>)
                    // 通常框架会预先初始化好这个集合，如果没初始化（即为 null），
                    // 在这种只读设计下通常会提供一个构造函数或初始化方法。
                    // 但绝大多数情况下，schema.Required 在此时已经被实例化了。
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