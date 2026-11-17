/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ColorService.cs
    File description: ColorService related implementation

    Created by: Senparc - 20250105

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.KnowledgeBase.Domain.Services;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.KnowledgeBase.Domain.Services
{
    public class ColorService : ServiceBase<Color>
    {
        public ColorService(IRepositoryBase<Color> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }

        public async Task<ColorDto> CreateNewColor()
        {
            Color color = new Color(-1, -1, -1);
            await base.SaveObjectAsync(color).ConfigureAwait(false);
            ColorDto colorDto = base.Mapper.Map<ColorDto>(color);
            return colorDto;
        }

        public async Task<ColorDto> GetOrInitColor()
        {
            var color = await base.GetObjectAsync(z => true);
            if (color == null)//Legacy comment translated to English.
            {
                //create default colors
                ColorDto colorDto = await this.CreateNewColor().ConfigureAwait(false);
                return colorDto;
            }

            return base.Mapper.Map<ColorDto>(color);
        }

        public async Task<ColorDto> Brighten()
        {
            //TODO: add sorting to the asynchronous method
            var obj = await this.GetObjectAsync(z => true, z => z.Id, OrderingType.Descending);
            obj.Brighten();
            await base.SaveObjectAsync(obj).ConfigureAwait(false);
            return base.Mapper.Map<ColorDto>(obj);
        }

        public async Task<ColorDto> Darken()
        {
            //TODO: add sorting to the asynchronous method
            var obj = await this.GetObjectAsync(z => true, z => z.Id, OrderingType.Descending);
            obj.Darken();
            await base.SaveObjectAsync(obj).ConfigureAwait(false);
            return base.Mapper.Map<ColorDto>(obj);
        }

        public async Task<ColorDto> Random()
        {
            //TODO: add sorting to the asynchronous method
            var obj = this.GetObject(z => true, z => z.Id, OrderingType.Descending);
            obj.Random();
            await base.SaveObjectAsync(obj).ConfigureAwait(false);
            return base.Mapper.Map<ColorDto>(obj);
        }

        //TODO: more business methods can be added here
    }
}
