/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AreaController.cs
    File description: Senparc.Mvc.Controllers related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261127
    Change description: v0.0.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Area;

namespace Senparc.Mvc.Controllers
{
    public class AreaController : BaseHttpsRequireController//NOT BaseController
    {
        /// <summary>
        /// Returns JSON area data for the requested province/city/district lookup.
        /// </summary>
        /// <param name="area">The area level to query.</param>
        /// <param name="name">The parent area name used by the lookup.</param>
        /// <returns>A JSON result containing the requested area data.</returns>
        [HttpPost]
        public ActionResult GetAreaJsonData(string area, string name)
        {
            var areaData = new AreaData();

            switch (area)
            {
                case "province":
                    var cityDataList = areaData.GetCitiesData(name);
                    return Json(cityDataList);
                case "city":
                    var districtDataList = areaData.GetDistrictsData(name);
                    return Json(districtDataList);
            }
            return null;
        }
    }
}
