//using Senparc.CO2NET;
//using Senparc.Ncf.Core.AppServices;
//using Senparc.Xncf.AIKernel.Domain.Services;
//using Senparc.Xncf.AIKernel.Models.DatabaseModel.Dto;
//using Senparc.Xncf.AIKernel.OHS.Local.PL;
//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading.Tasks;

//namespace Senparc.Xncf.AIKernel.OHS.Local.AppService
//{
//    public class ColorAppService : AppServiceBase
//    {
//        private readonly ColorService _colorService;

//        public ColorAppService(IServiceProvider serviceProvider, ColorService colorService) : base(serviceProvider)
//        {
//            this._colorService = colorService;
//        }


//        /// <summary>
//        /// Get or initialize a ColorDto object
//        /// </summary>
//        /// <returns></returns>
//        public async Task<AppResponseBase<Color_GetOrInitColorResponse>> GetOrInitColorAsync()
//        {
//            return await this.GetResponseAsync<Color_GetOrInitColorResponse>(async (response, logger) =>
//            {
//                var dt1 = SystemTime.Now;//Start timing

//                var colorDto = await _colorService.GetOrInitColor();//Get or initialize color parameters

//                var costMs = SystemTime.DiffTotalMS(dt1);//Record time consumption

//                Color_GetOrInitColorResponse result = new(colorDto.Red, colorDto.Green, colorDto.Blue, costMs);

//                return result;
//            });
//        }
//    }
//}
