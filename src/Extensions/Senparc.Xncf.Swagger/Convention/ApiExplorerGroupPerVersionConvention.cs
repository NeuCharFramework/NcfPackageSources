/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: ApiExplorerGroupPerVersionConvention.cs

    File description: ApiExplorerGroupPerVersionConvention related implementation

    

    

    Created by: Senparc - 20210614

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Linq;

namespace Senparc.Xncf.Swagger.Convention
{
    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// 
    ///Legacy comment translated to English.
    /// services.AddMvc(c =>
    ///     c.Conventions.Add(new ApiExplorerGroupPerVersionConvention())
    /// );
    /// </summary>
    public class ApiExplorerGroupPerVersionConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            var controllerNamespace = controller.ControllerType.Namespace; // e.g. "Controllers.V1"
            var apiVersion = controllerNamespace.Split('.').Last().ToLower();

            controller.ApiExplorer.GroupName = apiVersion;
        }
    }
}
