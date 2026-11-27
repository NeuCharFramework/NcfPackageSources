/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ColorService.cs
    File description: ColorService related implementation

    Created by: Senparc - 20250113

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.4.13 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.4.13.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.SenMapic.Domain.Services;
using Senparc.Xncf.SenMapic.Models.DatabaseModel.Dto;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.SenMapic.Domain.Services
{
    public class ColorService : ServiceBase<Color>
    {
        /// <summary>
        /// Initializes the service used to persist and project the sample
        /// <see cref="Color"/> entity.
        /// </summary>
        /// <param name="repo">The repository used for color queries and persistence.</param>
        /// <param name="serviceProvider">The service provider passed to the base service.</param>
        public ColorService(IRepositoryBase<Color> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// Creates a new color record with the entity's default-channel behavior,
        /// persists it, and maps the saved entity to a <see cref="ColorDto"/>.
        /// </summary>
        /// <returns>The newly persisted color represented as a DTO.</returns>
        /// <exception cref="Exception">
        /// Propagates repository or mapping failures raised while saving or projecting
        /// the color.
        /// </exception>
        public async Task<ColorDto> CreateNewColor()
        {
            Color color = new Color(-1, -1, -1);
            await base.SaveObjectAsync(color).ConfigureAwait(false);
            ColorDto colorDto = base.Mapper.Map<ColorDto>(color);
            return colorDto;
        }

        /// <summary>
        /// Retrieves the first available color record, or creates the initial record
        /// when the color table does not contain one.
        /// </summary>
        /// <returns>The existing or newly created color represented as a DTO.</returns>
        /// <exception cref="Exception">
        /// Propagates repository, persistence, or mapping failures raised by the
        /// underlying service operations.
        /// </exception>
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
        /// Loads the newest stored color, increases each RGB channel according to
        /// <see cref="Color.Brighten"/>, persists the change, and returns the updated DTO.
        /// </summary>
        /// <returns>The brightened color represented as a DTO.</returns>
        /// <exception cref="InvalidOperationException">
        /// May be raised by the underlying service when no color can be loaded.
        /// </exception>
        /// <exception cref="Exception">
        /// Propagates persistence or mapping failures.
        /// </exception>
        public async Task<ColorDto> Brighten()
        {
            //TODO: add sorting to the asynchronous method
            var obj = await this.GetObjectAsync(z => true, z => z.Id, OrderingType.Descending);
            obj.Brighten();
            await base.SaveObjectAsync(obj).ConfigureAwait(false);
            return base.Mapper.Map<ColorDto>(obj);
        }

        /// <summary>
        /// Loads the newest stored color, decreases each RGB channel according to
        /// <see cref="Color.Darken"/>, persists the change, and returns the updated DTO.
        /// </summary>
        /// <returns>The darkened color represented as a DTO.</returns>
        /// <exception cref="InvalidOperationException">
        /// May be raised by the underlying service when no color can be loaded.
        /// </exception>
        /// <exception cref="Exception">
        /// Propagates persistence or mapping failures.
        /// </exception>
        public async Task<ColorDto> Darken()
        {
            //TODO: add sorting to the asynchronous method
            var obj = await this.GetObjectAsync(z => true, z => z.Id, OrderingType.Descending);
            obj.Darken();
            await base.SaveObjectAsync(obj).ConfigureAwait(false);
            return base.Mapper.Map<ColorDto>(obj);
        }

        /// <summary>
        /// Loads the newest stored color, replaces its RGB channels using
        /// <see cref="Color.Random"/>, persists the change, and returns the updated DTO.
        /// </summary>
        /// <returns>The randomized color represented as a DTO.</returns>
        /// <exception cref="InvalidOperationException">
        /// May be raised by the underlying service when no color can be loaded.
        /// </exception>
        /// <exception cref="Exception">
        /// Propagates persistence or mapping failures.
        /// </exception>
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
