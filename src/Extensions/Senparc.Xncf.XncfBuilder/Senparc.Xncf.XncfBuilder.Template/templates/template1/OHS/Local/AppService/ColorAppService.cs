using Senparc.Ncf.Core.AppServices;
using Template_OrgName.Xncf.Template_XncfName.Domain.Services;
using Template_OrgName.Xncf.Template_XncfName.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Template_OrgName.Xncf.Template_XncfName.OHS.Local.AppService
{
    public class ColorAppService : AppServiceBase
    {
        private ColorService _colorService;
        public ColorAppService(IServiceProvider serviceProvider, ColorService colorService) : base(serviceProvider)
        {
            _colorService = colorService;
        }

        /// <summary>
        /// 获取或初始化一个 ColorDto 对象
        /// </summary>
        /// <returns></returns>
        public async Task<ColorDto> GetOrInitColorDtoAsync()
        {
            var color = _colorService.GetObject(z => true);
            if (color == null)//如果是纯第一次安装，理论上不会有残留数据
            {
                //创建默认颜色
                ColorDto colorDto = await _colorService.CreateNewColor().ConfigureAwait(false);
                return colorDto;
            }

            return _colorService.Mapper.Map<ColorDto>(color);
        }
    }
}
