/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ColorService.cs
    File description: ColorService related implementation

    Created by: Senparc - 20240718

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.7.12 Localized DynamicData configuration and layout surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.DynamicData.Domain.Services;
using Senparc.Xncf.DynamicData.Models.DatabaseModel.Dto;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.DynamicData.Domain.Services
{
    public class ColorService : ServiceBase<Color>
    {
        /// <summary>
        /// Initializes the color service with its repository and service provider.
        /// </summary>
        /// <param name="repo">The color repository.</param>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        public ColorService(IRepositoryBase<Color> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// Creates and persists a random color, then maps it to a DTO.
        /// </summary>
        /// <returns>The newly persisted random color.</returns>
        public async Task<ColorDto> CreateNewColor()
        {
            Color color = new Color(-1, -1, -1);
            await base.SaveObjectAsync(color).ConfigureAwait(false);
            ColorDto colorDto = base.Mapper.Map<ColorDto>(color);
            return colorDto;
        }

        /// <summary>
        /// Loads the newest color or creates a random initial color when none exists.
        /// </summary>
        /// <returns>The existing or newly created color DTO.</returns>
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

        /// <summary>
        /// Brightens the newest persisted color and saves the result.
        /// </summary>
        /// <returns>The updated color DTO.</returns>
        public async Task<ColorDto> Brighten()
        {
            //TODO: add sorting to the asynchronous method
            var obj = this.GetObject(z => true, z => z.Id, OrderingType.Descending);
            obj.Brighten();
            await base.SaveObjectAsync(obj).ConfigureAwait(false);
            return base.Mapper.Map<ColorDto>(obj);
        }

        /// <summary>
        /// Darkens the newest persisted color and saves the result.
        /// </summary>
        /// <returns>The updated color DTO.</returns>
        public async Task<ColorDto> Darken()
        {
            //TODO: add sorting to the asynchronous method
            var obj = this.GetObject(z => true, z => z.Id, OrderingType.Descending);
            obj.Darken();
            await base.SaveObjectAsync(obj).ConfigureAwait(false);
            return base.Mapper.Map<ColorDto>(obj);
        }

        /// <summary>
        /// Replaces the newest persisted color with a random color and saves the result.
        /// </summary>
        /// <returns>The updated color DTO.</returns>
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
