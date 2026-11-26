/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ColorService.cs
    File description: ColorService related implementation

    Created by: Senparc - 20250105

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.8.4 Localized knowledge base management and runtime surfaces

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
        /// <summary>
        /// Initializes the sample-color persistence service used by the KnowledgeBase
        /// database demonstration page.
        /// </summary>
        /// <param name="repo">The color repository.</param>
        /// <param name="serviceProvider">The dependency provider passed to the base service.</param>
        public ColorService(IRepositoryBase<Color> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// Creates, persists, and maps a new sample color using the entity's
        /// default-channel behavior.
        /// </summary>
        /// <returns>The newly persisted color DTO.</returns>
        public async Task<ColorDto> CreateNewColor()
        {
            Color color = new Color(-1, -1, -1);
            await base.SaveObjectAsync(color).ConfigureAwait(false);
            ColorDto colorDto = base.Mapper.Map<ColorDto>(color);
            return colorDto;
        }

        /// <summary>
        /// Retrieves the available sample color or creates the initial record when
        /// no color has yet been persisted.
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
        /// Increases the newest stored color's channels through
        /// <see cref="Color.Brighten"/> and persists the result.
        /// </summary>
        /// <returns>The brightened color DTO.</returns>
        public async Task<ColorDto> Brighten()
        {
            //TODO: add sorting to the asynchronous method
            var obj = await this.GetObjectAsync(z => true, z => z.Id, OrderingType.Descending);
            obj.Brighten();
            await base.SaveObjectAsync(obj).ConfigureAwait(false);
            return base.Mapper.Map<ColorDto>(obj);
        }

        /// <summary>
        /// Decreases the newest stored color's channels through
        /// <see cref="Color.Darken"/> and persists the result.
        /// </summary>
        /// <returns>The darkened color DTO.</returns>
        public async Task<ColorDto> Darken()
        {
            //TODO: add sorting to the asynchronous method
            var obj = await this.GetObjectAsync(z => true, z => z.Id, OrderingType.Descending);
            obj.Darken();
            await base.SaveObjectAsync(obj).ConfigureAwait(false);
            return base.Mapper.Map<ColorDto>(obj);
        }

        /// <summary>
        /// Replaces the newest stored color's channels through
        /// <see cref="Color.Random"/> and persists the result.
        /// </summary>
        /// <returns>The randomized color DTO.</returns>
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
