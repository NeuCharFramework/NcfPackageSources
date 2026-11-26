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
        /// <summary>Initializes the sample database page.</summary>
        /// <param name="serviceProvider">The request service provider.</param>
        /// <param name="colorService">The color service.</param>
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

        /// <summary>Loads the newest color, creating one when absent.</summary>
        /// <returns>A task that completes after page state is loaded.</returns>
        public async Task OnGetAsync()
        {
            var color = _colorService.GetObject(z => true, z => z.Id, OrderingType.Descending);
            ColorDto = color == null
                        ? (await _colorService.CreateNewColor())
                        : _colorService.Mapper.Map<ColorDto>(color);
        }

        /// <summary>Returns current color and module metadata as JSON.</summary>
        /// <returns>An HTTP 200 JSON result.</returns>
        public IActionResult OnGetDetail()
        {
            var color = _colorService.GetObject(z => true, z => z.Id, OrderingType.Descending);
            var colorDto = _colorService.Mapper.Map<ColorDto>(color);
            return Ok(new { colorDto, XncfModuleDto });
        }

        /// <summary>Brightens the stored color.</summary>
        /// <returns>An HTTP 200 result with the updated color.</returns>
        public async Task<IActionResult> OnGetBrightenAsync()
        {
            var colorDto = await _colorService.Brighten().ConfigureAwait(false);
            return Ok(colorDto);
        }

        /// <summary>Darkens the stored color.</summary>
        /// <returns>An HTTP 200 result with the updated color.</returns>
        public async Task<IActionResult> OnGetDarkenAsync()
        {
            var colorDto = await _colorService.Darken().ConfigureAwait(false);
            return Ok(colorDto);
        }
        /// <summary>Generates and persists a random color.</summary>
        /// <returns>An HTTP 200 result with the updated color.</returns>
        public async Task<IActionResult> OnGetRandomAsync()
        {
            var colorDto = await _colorService.Random().ConfigureAwait(false);
            return Ok(colorDto);
        }
    }
}
