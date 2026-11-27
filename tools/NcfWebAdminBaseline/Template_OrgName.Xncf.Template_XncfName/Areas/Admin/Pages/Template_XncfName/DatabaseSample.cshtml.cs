/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DatabaseSample.cshtml.cs
    File description: Template_OrgName.Xncf.Template_XncfName.Areas.Template_XncfName.Pages related implementation

    Created by: Senparc - 20200820

    Modified by: Senparc - 20261127
    Change description: v1.1.16 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Service;
using Template_OrgName.Xncf.Template_XncfName.Domain.Services;
using Template_OrgName.Xncf.Template_XncfName.Domain.Models.DatabaseModel.Dto;
using System;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Models;

namespace Template_OrgName.Xncf.Template_XncfName.Areas.Template_XncfName.Pages
{
    public class DatabaseSample : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public ColorDto ColorDto { get; set; }

        public MultipleDatabaseType MultipleDatabaseType { get; set; }

        private readonly ColorService _colorService;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes the database-sample page and captures the active database provider.
        /// </summary>
        /// <param name="serviceProvider">The request service provider.</param>
        /// <param name="colorService">The sample-color service.</param>
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
        /// Loads the newest sample color, creating one when no record exists.
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
        /// Returns the current sample color and module metadata.
        /// </summary>
        /// <returns>An HTTP 200 result containing the sample data.</returns>
        public IActionResult OnGetDetail()
        {
            var color = _colorService.GetObject(z => true, z => z.Id, OrderingType.Descending);
            var colorDto = _colorService.Mapper.Map<ColorDto>(color);
            return Ok(new { colorDto, XncfModuleDto });
        }

        /// <summary>
        /// Brightens the persisted sample color and returns the updated DTO.
        /// </summary>
        /// <returns>An HTTP 200 result containing the brightened color.</returns>
        public async Task<IActionResult> OnGetBrightenAsync()
        {
            var colorDto = await _colorService.Brighten().ConfigureAwait(false);
            return Ok(colorDto);
        }

        /// <summary>
        /// Darkens the persisted sample color and returns the updated DTO.
        /// </summary>
        /// <returns>An HTTP 200 result containing the darkened color.</returns>
        public async Task<IActionResult> OnGetDarkenAsync()
        {
            var colorDto = await _colorService.Darken().ConfigureAwait(false);
            return Ok(colorDto);
        }
        /// <summary>
        /// Randomizes the persisted sample color and returns the updated DTO.
        /// </summary>
        /// <returns>An HTTP 200 result containing the randomized color.</returns>
        public async Task<IActionResult> OnGetRandomAsync()
        {
            var colorDto = await _colorService.Random().ConfigureAwait(false);
            return Ok(colorDto);
        }
    }
}
