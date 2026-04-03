using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Linq;

namespace Senparc.Xncf.Swagger.Convention
{
    /// <summary>
    /// Assign operations to the document according to agreement
    /// You can follow the following convention to assign operations to documents based on the controller namespace
    /// 
    /// The following configuration needs to be added to Startup.ConfigureServices
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
