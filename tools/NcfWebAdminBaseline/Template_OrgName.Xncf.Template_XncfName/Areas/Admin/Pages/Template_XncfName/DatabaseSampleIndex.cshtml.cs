/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: DatabaseSampleIndex.cshtml.cs
    File description: DatabaseSampleIndex.cshtml related implementation

    Created by: Senparc - 20250524

    Modified by: Senparc - 20261119
    Change description: v1.1.15 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261124
    Change description: v1.1.15 Integrated localization, tooling, and runtime improvements across the module.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using Template_OrgName.Xncf.Template_XncfName.Domain.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Template_OrgName.Xncf.Template_XncfName.Domain.Models.DatabaseModel.Dto;

namespace Template_OrgName.Xncf.Template_XncfName.Areas.Template_XncfName.Pages
{
    public class DatabaseSampleIndex : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly ColorService _colorService;

        public DatabaseSampleIndex(Lazy<XncfModuleService> xncfModuleService, ColorService colorService) : base(xncfModuleService)
        {
            _colorService = colorService;
        }

        /// <summary>
        /// Handles the initial sample database page request.
        /// </summary>
        public void OnGet()
        {
        }

        /// <summary>
        /// Returns a paged list of sample colors using the requested sort expression.
        /// Database failures are converted into a failed JSON response.
        /// </summary>
        /// <param name="keyword">Reserved search text for the sample list.</param>
        /// <param name="orderField">The service-supported sort expression.</param>
        /// <param name="pageIndex">The one-based page number.</param>
        /// <param name="pageSize">The maximum number of rows per page.</param>
        /// <returns>An <see cref="OkObjectResult"/> containing paging metadata and color rows.</returns>
        public async Task<IActionResult> OnGetColorListAsync(string keyword, string orderField, int pageIndex, int pageSize)
        {
            try
            {
                // debug information
                System.Diagnostics.Debug.WriteLine($"ColorList API Called - PageIndex: {pageIndex}, PageSize: {pageSize}, OrderField: {orderField}");

                var seh = new SenparcExpressionHelper<Color>();
                //Legacy comment translated to English.
                // seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(keyword), _ => _.Remark.Contains(keyword));
                var where = seh.BuildWhereExpression();
                var response = await _colorService.GetObjectListAsync(pageIndex, pageSize, where, orderField ?? "Id desc");

                // debug information
                System.Diagnostics.Debug.WriteLine($"Database Query Result - TotalCount: {response.TotalCount}, ItemCount: {response.Count()}");

                var result = new
                {
                    success = true,
                    message = Template_XncfNameResource.Get("Frontend.Template_XncfName.DataLoadedSuccessfully", "Data loaded successfully"),
                    data = new {
                        totalCount = response.TotalCount,
                        pageIndex = response.PageIndex,
                        list = response.Select(_ => new
                        {
                            id = _.Id,
                            red = _.Red,
                            green = _.Green,
                            blue = _.Blue,
                            additionNote = _.AdditionNote,
                            addTime = _.AddTime,
                            lastUpdateTime = _.LastUpdateTime,
                            remark = _.Remark
                        }).ToList()
                    }
                };

                // debug information
                System.Diagnostics.Debug.WriteLine($"API Response - ListCount: {result.data.list.Count}");

                return Ok(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ColorList API Error: {ex.Message}");
                return Ok(new {
                    success = false,
                    message = Template_XncfNameResource.Format("Frontend.Template_XncfName.FailedToLoadDataValue", "Failed to load data: {0}", ex.Message),
                    totalCount = 0,
                    pageIndex = pageIndex,
                    list = new object[0]
                });
            }
        }

        /// <summary>
        /// Creates and persists a sample color from the JSON request.
        /// </summary>
        /// <param name="request">The RGB and note values for the new color.</param>
        /// <returns>An <see cref="OkObjectResult"/> indicating success or a localized failure message.</returns>
        public async Task<IActionResult> OnPostCreateColorAsync([FromBody] CreateColorRequestDto request)
        {
            try
            {
                // debug information
                System.Diagnostics.Debug.WriteLine($"CreateColor API Called - Red: {request.Red}, Green: {request.Green}, Blue: {request.Blue}, AdditionNote: {request.AdditionNote}");

                if (request == null)
                {
                    return Ok(new { success = false, message = Template_XncfNameResource.Get("Frontend.Template_XncfName.RequestParametersCannotBeEmpty", "Request parameters are required") });
                }

                var color = new Color(request.Red, request.Green, request.Blue, request.AdditionNote);
                await _colorService.SaveObjectAsync(color);

                return Ok(new { success = true, message = Template_XncfNameResource.Get("Frontend.Template_XncfName.ColorCreatedSuccessfully", "Color created"), data = new { color.Id, color.Red, color.Green, color.Blue, color.AdditionNote, color.AddTime, color.LastUpdateTime } });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateColor API Error: {ex.Message}");
                return Ok(new { success = false, message = Template_XncfNameResource.Format("Frontend.Template_XncfName.FailedToCreateValue", "Create failed: {0}", ex.Message) });
            }
        }

        /// <summary>
        /// Loads and updates an existing sample color from the JSON request.
        /// </summary>
        /// <param name="request">The identifier and replacement RGB/note values.</param>
        /// <returns>An <see cref="OkObjectResult"/> indicating success or a localized failure message.</returns>
        public async Task<IActionResult> OnPostUpdateColorAsync([FromBody] UpdateColorRequestDto request)
        {
            try
            {
                // debug information
                System.Diagnostics.Debug.WriteLine($"UpdateColor API Called - Id: {request.Id}, Red: {request.Red}, Green: {request.Green}, Blue: {request.Blue}, AdditionNote: {request.AdditionNote}");

                if (request == null)
                {
                    return Ok(new { success = false, message = Template_XncfNameResource.Get("Frontend.Template_XncfName.RequestParametersCannotBeEmpty", "Request parameters are required") });
                }

                var color = await _colorService.GetObjectAsync(c => c.Id == request.Id);
                if (color == null)
                {
                    return Ok(new { success = false, message = Template_XncfNameResource.Get("Frontend.Template_XncfName.TheColorDoesNotExist", "Color not found") });
                }

                // update
                color.Update(request);

                await _colorService.SaveObjectAsync(color);

                return Ok(new { success = true, message = Template_XncfNameResource.Get("Frontend.Template_XncfName.ColorUpdatedSuccessfully", "Color updated"), data = new { color.Id, color.Red, color.Green, color.Blue, color.AdditionNote, color.LastUpdateTime } });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateColor API Error: {ex.Message}");
                return Ok(new { success = false, message = Template_XncfNameResource.Format("Frontend.Template_XncfName.UpdateFailedValue", "Update failed: {0}", ex.Message) });
            }
        }

        /// <summary>
        /// Deletes the sample color identified by the JSON request.
        /// </summary>
        /// <param name="request">The identifier of the color to delete.</param>
        /// <returns>An <see cref="OkObjectResult"/> indicating success or a localized failure message.</returns>
        public async Task<IActionResult> OnPostDeleteColorAsync([FromBody] DeleteColorRequestDto request)
        {
            try
            {
                // debug information
                System.Diagnostics.Debug.WriteLine($"DeleteColor API Called - Id: {request.Id}");

                if (request == null)
                {
                    return Ok(new { success = false, message = Template_XncfNameResource.Get("Frontend.Template_XncfName.RequestParametersCannotBeEmpty", "Request parameters are required") });
                }

                var color = await _colorService.GetObjectAsync(c => c.Id == request.Id);
                if (color == null)
                {
                    return Ok(new { success = false, message = Template_XncfNameResource.Get("Frontend.Template_XncfName.TheColorDoesNotExist", "Color not found") });
                }

                await _colorService.DeleteObjectAsync(color);
                return Ok(new { success = true, message = Template_XncfNameResource.Get("Frontend.Template_XncfName.ColorDeletedSuccessfully", "Color deleted") });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteColor API Error: {ex.Message}");
                return Ok(new { success = false, message = Template_XncfNameResource.Format("Frontend.Template_XncfName.FailedToDeleteValue", "Delete failed: {0}", ex.Message) });
            }
        }

        /// <summary>
        /// Generates and persists a new random RGB value for an existing sample color.
        /// </summary>
        /// <param name="request">The identifier of the color to randomize.</param>
        /// <returns>An <see cref="OkObjectResult"/> containing the randomized color or a failure message.</returns>
        public async Task<IActionResult> OnPostRandomizeColorAsync([FromBody] RandomizeColorRequestDto request)
        {
            try
            {
                // debug information
                System.Diagnostics.Debug.WriteLine($"RandomizeColor API Called - Id: {request.Id}");

                if (request == null)
                {
                    return Ok(new { success = false, message = Template_XncfNameResource.Get("Frontend.Template_XncfName.RequestParametersCannotBeEmpty", "Request parameters are required") });
                }

                var color = await _colorService.GetObjectAsync(c => c.Id == request.Id);
                if (color == null)
                {
                    return Ok(new { success = false, message = Template_XncfNameResource.Get("Frontend.Template_XncfName.TheColorDoesNotExist", "Color not found") });
                }

                color.Random();
                await _colorService.SaveObjectAsync(color);

                return Ok(new { success = true, message = Template_XncfNameResource.Get("Frontend.Template_XncfName.ColorRandomizedSuccessfully", "Color randomized"), data = new { color.Id, color.Red, color.Green, color.Blue, color.AdditionNote, color.LastUpdateTime } });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RandomizeColor API Error: {ex.Message}");
                return Ok(new { success = false, message = Template_XncfNameResource.Format("Frontend.Template_XncfName.RandomizationFailedValue", "Randomization failed: {0}", ex.Message) });
            }
        }

        /// <summary>
        /// Retrieves one sample color by identifier for the edit view.
        /// </summary>
        /// <param name="id">colorID</param>
        /// <returns>An <see cref="OkObjectResult"/> containing the color data or a failure message.</returns>
        public async Task<IActionResult> OnGetColorDetailAsync(int id)
        {
            try
            {
                var color = await _colorService.GetObjectAsync(c => c.Id == id);
                if (color == null)
                {
                    return Ok(new { success = false, message = Template_XncfNameResource.Get("Frontend.Template_XncfName.TheColorDoesNotExist", "Color not found") });
                }

                return Ok(new { success = true, data = new { color.Id, color.Red, color.Green, color.Blue, color.AdditionNote, color.AddTime, color.LastUpdateTime, color.Remark } });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = Template_XncfNameResource.Format("Frontend.Template_XncfName.FailedToLoadValue", "Load failed: {0}", ex.Message) });
            }
        }
    }
}
