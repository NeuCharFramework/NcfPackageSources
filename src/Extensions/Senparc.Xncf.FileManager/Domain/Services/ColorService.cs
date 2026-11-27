/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ColorService.cs
    File description: ColorService related implementation

    Created by: Senparc - 20250105

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.8.5 Localized file management and upload surfaces

    Modified by: Senparc - 20261127
    Change description: v0.8.5.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.FileManager.Domain.Services;
using Senparc.Xncf.FileManager.Models.DatabaseModel.Dto;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.FileManager.Domain.Services
{
    public class ColorService : ServiceBase<Color>
    {
        /// <summary>
        /// Initializes the color persistence service.
        /// </summary>
        /// <param name="repo">The repository used to load and save colors.</param>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        public ColorService(IRepositoryBase<Color> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// Creates and persists a new randomly initialized color.
        /// </summary>
        /// <returns>The persisted color mapped to a DTO.</returns>
        public async Task<ColorDto> CreateNewColor()
        {
            Color color = new Color(-1, -1, -1);
            await base.SaveObjectAsync(color).ConfigureAwait(false);
            ColorDto colorDto = base.Mapper.Map<ColorDto>(color);
            return colorDto;
        }

        /// <summary>
        /// Loads a stored color or creates the initial random color when no record exists.
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
        /// Brightens the newest stored color and persists the updated RGB values.
        /// </summary>
        /// <returns>The updated color DTO.</returns>
        public async Task<ColorDto> Brighten()
        {
            //TODO: add sorting to the asynchronous method
            var obj = await this.GetObjectAsync(z => true, z => z.Id, OrderingType.Descending);
            obj.Brighten();
            await base.SaveObjectAsync(obj).ConfigureAwait(false);
            return base.Mapper.Map<ColorDto>(obj);
        }

        /// <summary>
        /// Darkens the newest stored color and persists the updated RGB values.
        /// </summary>
        /// <returns>The updated color DTO.</returns>
        public async Task<ColorDto> Darken()
        {
            //TODO: add sorting to the asynchronous method
            var obj = await this.GetObjectAsync(z => true, z => z.Id, OrderingType.Descending);
            obj.Darken();
            await base.SaveObjectAsync(obj).ConfigureAwait(false);
            return base.Mapper.Map<ColorDto>(obj);
        }

        /// <summary>
        /// Replaces the newest stored color with a generated random color and persists it.
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
