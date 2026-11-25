/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ColorService.cs
    File description: ColorService related implementation

    Created by: Senparc - 20240311

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.13.16 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AIAgentsHub.Domain.Services;
using Senparc.Xncf.AIAgentsHub.Models.DatabaseModel.Dto;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.AIAgentsHub.Domain.Services
{
    public class ColorService : ServiceBase<Color>
    {
        /// <summary>
        /// Initializes the color persistence service.
        /// </summary>
        /// <param name="repo">Repository for color records.</param>
        /// <param name="serviceProvider">Dependency-injection provider.</param>
        public ColorService(IRepositoryBase<Color> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// Creates and persists a new color initialized with neutral channel values.
        /// </summary>
        /// <returns>The created color DTO.</returns>
        public async Task<ColorDto> CreateNewColor()
        {
            Color color = new Color(-1, -1, -1);
            await base.SaveObjectAsync(color).ConfigureAwait(false);
            ColorDto colorDto = base.Mapper.Map<ColorDto>(color);
            return colorDto;
        }

        /// <summary>
        /// Retrieves the newest color or creates the initial color when none exists.
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
        /// Brightens the newest stored color and persists the result.
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
        /// Darkens the newest stored color and persists the result.
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
        /// Replaces the newest stored color with a generated random color.
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
