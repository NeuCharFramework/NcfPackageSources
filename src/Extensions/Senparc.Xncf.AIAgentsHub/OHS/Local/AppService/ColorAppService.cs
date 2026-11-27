/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ColorAppService.cs
    File description: ColorAppService related implementation

    Created by: Senparc - 20240311

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.13.16 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.13.16.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.AIAgentsHub.Domain.Services;
using Senparc.Xncf.AIAgentsHub.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIAgentsHub.OHS.Local.PL;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AIAgentsHub.OHS.Local.AppService
{
    public class ColorAppService : AppServiceBase
    {
        private readonly ColorService _colorService;

        /// <summary>
        /// Initializes the color application service.
        /// </summary>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        /// <param name="colorService">The color persistence service.</param>
        public ColorAppService(IServiceProvider serviceProvider, ColorService colorService) : base(serviceProvider)
        {
            this._colorService = colorService;
        }


        /// <summary>
        /// Gets the newest persisted color or creates the initial color.
        /// </summary>
        /// <returns>An application response containing RGB values and elapsed operation time.</returns>
        public async Task<AppResponseBase<Color_GetOrInitColorResponse>> GetOrInitColorAsync()
        {
            return await this.GetResponseAsync<Color_GetOrInitColorResponse>(async (response, logger) =>
            {
                var dt1 = SystemTime.Now;//Legacy comment translated to English.

                var colorDto = await _colorService.GetOrInitColor();//getorinitializecolorparameters

                var costMs = SystemTime.DiffTotalMS(dt1);//Legacy comment translated to English.

                Color_GetOrInitColorResponse result = new(colorDto.Red, colorDto.Green, colorDto.Blue, costMs);

                return result;
            });
        }
    }
}
