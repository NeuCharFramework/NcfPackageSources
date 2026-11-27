/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ApiExplorerGroupPerVersionConvention.cs
    File description: ApiExplorerGroupPerVersionConvention related implementation

    Created by: Senparc - 20210614

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.12 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.20.12.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Linq;

namespace Senparc.Xncf.Swagger.Convention
{
    /// <summary>
    /// Groups API Explorer descriptions by the final namespace segment, which is
    /// conventionally the controller's API version.
    /// </summary>
    public class ApiExplorerGroupPerVersionConvention : IControllerModelConvention
    {
        /// <summary>
        /// Assigns the controller's namespace-derived version to its API Explorer group.
        /// </summary>
        /// <param name="controller">The controller model being configured.</param>
        public void Apply(ControllerModel controller)
        {
            var controllerNamespace = controller.ControllerType.Namespace; // e.g. "Controllers.V1"
            var apiVersion = controllerNamespace.Split('.').Last().ToLower();

            controller.ApiExplorer.GroupName = apiVersion;
        }
    }
}
