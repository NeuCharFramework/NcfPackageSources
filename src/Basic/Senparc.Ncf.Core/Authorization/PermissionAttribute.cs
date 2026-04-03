using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Senparc.Ncf.Core.Authorization
{
    /// <summary>
    ///permissions properties
    /// Usage [Permission("role.add,role.update")]
    /// </summary>
    public class PermissionAttribute : TypeFilterAttribute
    {
        //public string[] Codes { get; set; }

        /// <summary>
        //English commas cannot exist in / code TODO
        /// </summary>
        /// <param name="codes">Multiple codes separated by English commas</param>
        public PermissionAttribute(string codes)
            : base(typeof(PermissionFilterAttribute))
        {
            //Arguments = new[] { new PermissionRequirement(Codes) };
            Arguments = new[] { new PermissionRequirement(codes.Split(",")) };
        }
    }
}
