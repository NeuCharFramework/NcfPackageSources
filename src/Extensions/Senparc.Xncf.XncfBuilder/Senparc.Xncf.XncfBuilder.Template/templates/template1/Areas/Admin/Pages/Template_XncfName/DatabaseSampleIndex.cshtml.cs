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

        public void OnGet()
        {
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <returns></returns>
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
                    message = Template_XncfNameResource.Get("Frontend.048", "Data loaded successfully"),
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
                    message = Template_XncfNameResource.Format("Frontend.037", "Failed to load data: {0}", ex.Message),
                    totalCount = 0,
                    pageIndex = pageIndex,
                    list = new object[0]
                });
            }
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="request">createcolorrequest</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostCreateColorAsync([FromBody] CreateColorRequestDto request)
        {
            try
            {
                // debug information
                System.Diagnostics.Debug.WriteLine($"CreateColor API Called - Red: {request.Red}, Green: {request.Green}, Blue: {request.Blue}, AdditionNote: {request.AdditionNote}");

                if (request == null)
                {
                    return Ok(new { success = false, message = Template_XncfNameResource.Get("Frontend.050", "Request parameters are required") });
                }

                var color = new Color(request.Red, request.Green, request.Blue, request.AdditionNote);
                await _colorService.SaveObjectAsync(color);

                return Ok(new { success = true, message = Template_XncfNameResource.Get("Frontend.051", "Color created"), data = new { color.Id, color.Red, color.Green, color.Blue, color.AdditionNote, color.AddTime, color.LastUpdateTime } });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateColor API Error: {ex.Message}");
                return Ok(new { success = false, message = Template_XncfNameResource.Format("Frontend.052", "Create failed: {0}", ex.Message) });
            }
        }

        /// <summary>
        /// updatecolor
        /// </summary>
        /// <param name="request">updatecolorrequest</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostUpdateColorAsync([FromBody] UpdateColorRequestDto request)
        {
            try
            {
                // debug information
                System.Diagnostics.Debug.WriteLine($"UpdateColor API Called - Id: {request.Id}, Red: {request.Red}, Green: {request.Green}, Blue: {request.Blue}, AdditionNote: {request.AdditionNote}");

                if (request == null)
                {
                    return Ok(new { success = false, message = Template_XncfNameResource.Get("Frontend.050", "Request parameters are required") });
                }

                var color = await _colorService.GetObjectAsync(c => c.Id == request.Id);
                if (color == null)
                {
                    return Ok(new { success = false, message = Template_XncfNameResource.Get("Frontend.053", "Color not found") });
                }

                // update
                color.Update(request);

                await _colorService.SaveObjectAsync(color);

                return Ok(new { success = true, message = Template_XncfNameResource.Get("Frontend.054", "Color updated"), data = new { color.Id, color.Red, color.Green, color.Blue, color.AdditionNote, color.LastUpdateTime } });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateColor API Error: {ex.Message}");
                return Ok(new { success = false, message = Template_XncfNameResource.Format("Frontend.055", "Update failed: {0}", ex.Message) });
            }
        }

        /// <summary>
        /// deletecolor
        /// </summary>
        /// <param name="request">deletecolorrequest</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostDeleteColorAsync([FromBody] DeleteColorRequestDto request)
        {
            try
            {
                // debug information
                System.Diagnostics.Debug.WriteLine($"DeleteColor API Called - Id: {request.Id}");

                if (request == null)
                {
                    return Ok(new { success = false, message = Template_XncfNameResource.Get("Frontend.050", "Request parameters are required") });
                }

                var color = await _colorService.GetObjectAsync(c => c.Id == request.Id);
                if (color == null)
                {
                    return Ok(new { success = false, message = Template_XncfNameResource.Get("Frontend.053", "Color not found") });
                }

                await _colorService.DeleteObjectAsync(color);
                return Ok(new { success = true, message = Template_XncfNameResource.Get("Frontend.056", "Color deleted") });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteColor API Error: {ex.Message}");
                return Ok(new { success = false, message = Template_XncfNameResource.Format("Frontend.057", "Delete failed: {0}", ex.Message) });
            }
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        /// <returns></returns>
        public async Task<IActionResult> OnPostRandomizeColorAsync([FromBody] RandomizeColorRequestDto request)
        {
            try
            {
                // debug information
                System.Diagnostics.Debug.WriteLine($"RandomizeColor API Called - Id: {request.Id}");

                if (request == null)
                {
                    return Ok(new { success = false, message = Template_XncfNameResource.Get("Frontend.050", "Request parameters are required") });
                }

                var color = await _colorService.GetObjectAsync(c => c.Id == request.Id);
                if (color == null)
                {
                    return Ok(new { success = false, message = Template_XncfNameResource.Get("Frontend.053", "Color not found") });
                }

                color.Random();
                await _colorService.SaveObjectAsync(color);

                return Ok(new { success = true, message = Template_XncfNameResource.Get("Frontend.058", "Color randomized"), data = new { color.Id, color.Red, color.Green, color.Blue, color.AdditionNote, color.LastUpdateTime } });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RandomizeColor API Error: {ex.Message}");
                return Ok(new { success = false, message = Template_XncfNameResource.Format("Frontend.059", "Randomization failed: {0}", ex.Message) });
            }
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="id">colorID</param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetColorDetailAsync(int id)
        {
            try
            {
                var color = await _colorService.GetObjectAsync(c => c.Id == id);
                if (color == null)
                {
                    return Ok(new { success = false, message = Template_XncfNameResource.Get("Frontend.053", "Color not found") });
                }

                return Ok(new { success = true, data = new { color.Id, color.Red, color.Green, color.Blue, color.AdditionNote, color.AddTime, color.LastUpdateTime, color.Remark } });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = Template_XncfNameResource.Format("Frontend.060", "Load failed: {0}", ex.Message) });
            }
        }
    }
}
