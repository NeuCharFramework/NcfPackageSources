/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DatabaseSample.cshtml.cs
    File description: DatabaseSample.cshtml related implementation

    Created by: Senparc - 20240718

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.7.12 Localized DynamicData configuration and layout surfaces

    Modified by: Senparc - 20261127
    Change description: v0.7.12.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Service;
using Senparc.Xncf.DynamicData.Domain.Services;
using Senparc.Xncf.DynamicData.Models.DatabaseModel.Dto;
using System;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.DynamicData.Areas.DynamicData.Pages
{
    public class DatabaseSample : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public ColorDto ColorDto { get; set; }

        public MultipleDatabaseType MultipleDatabaseType { get; set; }

        private readonly ColorService _colorService;
        private readonly IServiceProvider _serviceProvider;
        /// <summary>
        /// Initializes the DynamicData database sample page and resolves the active database provider.
        /// </summary>
        /// <param name="serviceProvider">The request service provider.</param>
        /// <param name="colorService">The sample color service.</param>
        /// <param name="xncfModuleService">The lazy XNCF module service.</param>
        public DatabaseSample(IServiceProvider serviceProvider, ColorService colorService, Lazy<XncfModuleService> xncfModuleService)
            : base(xncfModuleService)
        {
            _colorService = colorService;
            _serviceProvider = serviceProvider;

            var databaseConfigurationFactory = DatabaseConfigurationFactory.Instance;
            var currentDatabaseConfiguration = databaseConfigurationFactory.Current;
            MultipleDatabaseType = currentDatabaseConfiguration.MultipleDatabaseType;
        }

        /// <summary>
        /// Loads the newest color record, creating an initial record when none exists.
        /// </summary>
        public async Task OnGetAsync()
        {
            var color = _colorService.GetObject(z => true, z => z.Id, OrderingType.Descending);
            ColorDto = color == null
                        ? (await _colorService.CreateNewColor())
                        : _colorService.Mapper.Map<ColorDto>(color);
        }

        /// <summary>
        /// Returns the current color and module metadata as JSON.
        /// </summary>
        /// <returns>An HTTP 200 JSON result.</returns>
        public IActionResult OnGetDetail()
        {
            var color = _colorService.GetObject(z => true, z => z.Id, OrderingType.Descending);
            var colorDto = _colorService.Mapper.Map<ColorDto>(color);
            return Ok(new { colorDto, XncfModuleDto });
        }

        /// <summary>
        /// Brightens the newest persisted color and returns the updated DTO.
        /// </summary>
        /// <returns>An HTTP 200 result containing the updated color.</returns>
        public async Task<IActionResult> OnGetBrightenAsync()
        {
            var colorDto = await _colorService.Brighten().ConfigureAwait(false);
            return Ok(colorDto);
        }

        /// <summary>
        /// Darkens the newest persisted color and returns the updated DTO.
        /// </summary>
        /// <returns>An HTTP 200 result containing the updated color.</returns>
        public async Task<IActionResult> OnGetDarkenAsync()
        {
            var colorDto = await _colorService.Darken().ConfigureAwait(false);
            return Ok(colorDto);
        }
        /// <summary>
        /// Replaces the newest persisted color with generated values and returns the updated DTO.
        /// </summary>
        /// <returns>An HTTP 200 result containing the updated color.</returns>
        public async Task<IActionResult> OnGetRandomAsync()
        {
            var colorDto = await _colorService.Random().ConfigureAwait(false);
            return Ok(colorDto);
        }
    }
}
