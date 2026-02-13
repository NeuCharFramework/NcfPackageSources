using Microsoft.OpenApi; // 10.0.0 版本核心命名空间
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Collections.Generic;

namespace Senparc.Xncf.Swagger.Filters
{
    /// <summary>
    /// 添加 swagger 接口版本默认值筛选器 (适配 10.0.0 只读属性版本)
    /// </summary>
    public class SwaggerDefaultValueFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null || operation.Parameters.Count == 0)
            {
                return;
            }

            // 因为要替换集合中的元素，使用 for 循环通过索引操作
            for (int i = 0; i < operation.Parameters.Count; i++)
            {
                var parameter = operation.Parameters[i];

                // 查找对应的 API 描述信息
                var description = context.ApiDescription.ParameterDescriptions
                                    .FirstOrDefault(p => p.Name == parameter.Name);

                if (description == null) continue;

                // 计算新的属性值
                var newDescription = parameter.Description ?? description.ModelMetadata?.Description;
                var newRequired = parameter.Required;

                if (description.RouteInfo != null)
                {
                    newRequired |= !description.RouteInfo.IsOptional;
                }

                // 判断是否需要“修改”（即替换对象）
                // 如果值没变，则无需替换以保持性能
                if (newDescription != parameter.Description || newRequired != parameter.Required)
                {
                    // 创建新实例，并从旧实例拷贝所有不需要改动的属性
                    // 在 10.0.0 版本中，这些属性通过对象初始化器设置
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
                        // 设置新值
                        Description = newDescription,
                        Required = newRequired
                    };
                }
            }
        }
    }
}