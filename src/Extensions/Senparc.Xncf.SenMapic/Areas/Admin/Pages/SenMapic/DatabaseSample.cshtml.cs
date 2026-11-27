/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DatabaseSample.cshtml.cs
    File description: DatabaseSample.cshtml related implementation

    Created by: Senparc - 20250113

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.4.13 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.4.13.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Service;
using Senparc.Xncf.SenMapic.Domain.Services;
using Senparc.Xncf.SenMapic.Models.DatabaseModel.Dto;
using System;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.SenMapic.Areas.SenMapic.Pages
{
    public class DatabaseSample : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public ColorDto ColorDto { get; set; }

        public MultipleDatabaseType MultipleDatabaseType { get; set; }

        private readonly ColorService _colorService;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes the database-sample page and captures the active multiple-
        /// database provider for display.
        /// </summary>
        /// <param name="serviceProvider">The request service provider.</param>
        /// <param name="colorService">The service used to read and mutate sample colors.</param>
        /// <param name="xncfModuleService">The lazy module service required by the base page.</param>
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
        /// Loads the newest sample color, creating one when the sample table is empty,
        /// so the Razor page can render the current persisted value.
        /// </summary>
        /// <returns>A task that completes after <see cref="ColorDto"/> is populated.</returns>
        public async Task OnGetAsync()
        {
            var color = _colorService.GetObject(z => true, z => z.Id, OrderingType.Descending);
            ColorDto = color == null
                        ? (await _colorService.CreateNewColor())
                        : _colorService.Mapper.Map<ColorDto>(color);
        }

        /// <summary>
        /// Returns the current sample color together with the module metadata expected
        /// by the database-sample client.
        /// </summary>
        /// <returns>An HTTP 200 result containing color and module DTO data.</returns>
        public IActionResult OnGetDetail()
        {
            var color = _colorService.GetObject(z => true, z => z.Id, OrderingType.Descending);
            var colorDto = _colorService.Mapper.Map<ColorDto>(color);
            return Ok(new { colorDto, XncfModuleDto });
        }

        /// <summary>
        /// Increases the persisted sample color channels and returns the updated DTO.
        /// </summary>
        /// <returns>An HTTP 200 result containing the brightened color.</returns>
        public async Task<IActionResult> OnGetBrightenAsync()
        {
            var colorDto = await _colorService.Brighten().ConfigureAwait(false);
            return Ok(colorDto);
        }

        /// <summary>
        /// Decreases the persisted sample color channels and returns the updated DTO.
        /// </summary>
        /// <returns>An HTTP 200 result containing the darkened color.</returns>
        public async Task<IActionResult> OnGetDarkenAsync()
        {
            var colorDto = await _colorService.Darken().ConfigureAwait(false);
            return Ok(colorDto);
        }

        /// <summary>
        /// Replaces the persisted sample color with generated channel values and
        /// returns the updated DTO.
        /// </summary>
        /// <returns>An HTTP 200 result containing the randomized color.</returns>
        public async Task<IActionResult> OnGetRandomAsync()
        {
            var colorDto = await _colorService.Random().ConfigureAwait(false);
            return Ok(colorDto);
        }
    }
}
