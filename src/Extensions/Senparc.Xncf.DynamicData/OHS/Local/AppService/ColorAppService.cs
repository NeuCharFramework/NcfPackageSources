/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ColorAppService.cs
    File description: ColorAppService related implementation

    Created by: Senparc - 20240718

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.7.12 Localized DynamicData configuration and layout surfaces

----------------------------------------------------------------*/

using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.DynamicData.Domain.Services;
using Senparc.Xncf.DynamicData.Models.DatabaseModel.Dto;
using Senparc.Xncf.DynamicData.OHS.Local.PL;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DynamicData.OHS.Local.AppService
{
    public class ColorAppService : AppServiceBase
    {
        private readonly ColorService _colorService;

        /// <summary>
        /// Initializes the color application service.
        /// </summary>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        /// <param name="colorService">The service used to load or initialize colors.</param>
        public ColorAppService(IServiceProvider serviceProvider, ColorService colorService) : base(serviceProvider)
        {
            this._colorService = colorService;
        }


        /// <summary>
        /// Gets the newest color or creates one when the color store is empty.
        /// </summary>
        /// <returns>An application response containing RGB values and operation duration.</returns>
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
