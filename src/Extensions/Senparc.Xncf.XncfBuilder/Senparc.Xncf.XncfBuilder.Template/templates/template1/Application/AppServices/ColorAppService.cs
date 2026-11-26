/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ColorAppService.cs
    File description: XNCF template sample implementation

    Created by: Senparc - 20211031

    Modified by: Senparc - 20260726
    Change description: v1.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v1.1.15 Localized XNCF Builder tools and template surfaces

----------------------------------------------------------------*/
using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Template_OrgName.Xncf.Template_XncfName.Domain.Services;
using Template_OrgName.Xncf.Template_XncfName.Domain.Models.DatabaseModel.Dto;
using Template_OrgName.Xncf.Template_XncfName.Application.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Template_OrgName.Xncf.Template_XncfName.Application.AppServices
{
    public class ColorAppService : AppServiceBase
    {
        private readonly ColorService _colorService;

        /// <summary>Initializes the template color application service.</summary>
        /// <param name="serviceProvider">The dependency provider.</param>
        /// <param name="colorService">The color service.</param>
        public ColorAppService(IServiceProvider serviceProvider, ColorService colorService) : base(serviceProvider)
        {
            this._colorService = colorService;
        }


        /// <summary>
        /// Gets the stored color or initializes one and returns its RGB values.
        /// </summary>
        /// <returns>An application response containing color values and elapsed time.</returns>
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
