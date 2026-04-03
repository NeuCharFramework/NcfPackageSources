using Microsoft.OpenApi; // 10.0.0 version core namespace
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Collections.Generic;

namespace Senparc.Xncf.Swagger.Filters
{
    /// <summary>
    /// Add swagger interface version default value filter (adapted to 10.0.0 read-only attribute version)
    /// </summary>
    public class SwaggerDefaultValueFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null || operation.Parameters.Count == 0)
            {
                return;
            }

            // Because you want to replace elements in the collection, use a for loop to operate by indexing
            for (int i = 0; i < operation.Parameters.Count; i++)
            {
                var parameter = operation.Parameters[i];

                // Find the corresponding API description information
                var description = context.ApiDescription.ParameterDescriptions
                                    .FirstOrDefault(p => p.Name == parameter.Name);

                if (description == null) continue;

                // Calculate new attribute values
                var newDescription = parameter.Description ?? description.ModelMetadata?.Description;
                var newRequired = parameter.Required;

                if (description.RouteInfo != null)
                {
                    newRequired |= !description.RouteInfo.IsOptional;
                }

                // Determine whether "modification" (i.e. replacement of the object) is required
                // If the value has not changed, no replacement is needed to maintain performance
                if (newDescription != parameter.Description || newRequired != parameter.Required)
                {
                    // Create a new instance and copy all properties from the old instance that do not need to be changed
                    // In version 10.0.0, these properties are set via object initializers
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
                        // Set new value
                        Description = newDescription,
                        Required = newRequired
                    };
                }
            }
        }
    }
}