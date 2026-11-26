/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ColorAppService.cs
    File description: ColorAppService related implementation

    Created by: Senparc - 20250105

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.8.5 Localized file management and upload surfaces

----------------------------------------------------------------*/

using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.FileManager.Domain.Services;
using Senparc.Xncf.FileManager.Models.DatabaseModel.Dto;
using Senparc.Xncf.FileManager.OHS.Local.PL;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.FileManager.OHS.Local.AppService
{
    public class ColorAppService : AppServiceBase
    {
        private readonly ColorService _colorService;

        /// <summary>
        /// Initializes the FileManager color application service.
        /// </summary>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        /// <param name="colorService">The color persistence service.</param>
        public ColorAppService(IServiceProvider serviceProvider, ColorService colorService) : base(serviceProvider)
        {
            this._colorService = colorService;
        }


        /// <summary>
        /// Gets the persisted color or initializes one and returns its RGB values.
        /// </summary>
        /// <returns>An application response containing color values and elapsed time.</returns>
        public async Task<AppResponseBase<Color_GetOrInitColorResponse>> GetOrInitColorAsync()
        {
            return await this.GetResponseAsync<Color_GetOrInitColorResponse>(async (response, logger) =>
            {
                var dt1 = SystemTime.Now;

                var colorDto = await _colorService.GetOrInitColor();

                var costMs = SystemTime.DiffTotalMS(dt1);

                Color_GetOrInitColorResponse result = new(colorDto.Red, colorDto.Green, colorDto.Blue, costMs);

                return result;
            });
        }
    }
}
