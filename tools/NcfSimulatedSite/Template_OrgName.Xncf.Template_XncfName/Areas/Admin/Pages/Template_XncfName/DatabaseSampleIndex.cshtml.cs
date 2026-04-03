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
        /// Get color list (pagination)
        /// </summary>
        /// <param name="keyword">Keyword</param>
        /// <param name="orderField">Order field</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetColorListAsync(string keyword, string orderField, int pageIndex, int pageSize)
        {
            try
            {
                // debugging information
                System.Diagnostics.Debug.WriteLine($"ColorList API Called - PageIndex: {pageIndex}, PageSize: {pageSize}, OrderField: {orderField}");
                
                var seh = new SenparcExpressionHelper<Color>();
                // You can add search conditions as needed
                // seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(keyword), _ => _.Remark.Contains(keyword));
                var where = seh.BuildWhereExpression();
                var response = await _colorService.GetObjectListAsync(pageIndex, pageSize, where, orderField ?? "Id desc");
                
                // debugging information
                System.Diagnostics.Debug.WriteLine($"Database Query Result - TotalCount: {response.TotalCount}, ItemCount: {response.Count()}");
                
                var result = new
                {
                    success = true,
                    message = "数据获取成功",
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
                
                // debugging information
                System.Diagnostics.Debug.WriteLine($"API Response - ListCount: {result.data.list.Count}");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ColorList API Error: {ex.Message}");
                return Ok(new { 
                    success = false, 
                    message = "获取数据失败: " + ex.Message,
                    totalCount = 0,
                    pageIndex = pageIndex,
                    list = new object[0]
                });
            }
        }

        /// <summary>
        ///Create new color
        /// </summary>
        /// <param name="request">Create color request</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostCreateColorAsync([FromBody] CreateColorRequestDto request)
        {
            try
            {
                // debugging information
                System.Diagnostics.Debug.WriteLine($"CreateColor API Called - Red: {request.Red}, Green: {request.Green}, Blue: {request.Blue}, AdditionNote: {request.AdditionNote}");
                
                if (request == null)
                {
                    return Ok(new { success = false, message = "请求参数不能为空" });
                }
                
                var color = new Color(request.Red, request.Green, request.Blue, request.AdditionNote);
                await _colorService.SaveObjectAsync(color);
                
                return Ok(new { success = true, message = "颜色创建成功", data = new { color.Id, color.Red, color.Green, color.Blue, color.AdditionNote, color.AddTime, color.LastUpdateTime } });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateColor API Error: {ex.Message}");
                return Ok(new { success = false, message = "创建失败：" + ex.Message });
            }
        }

        /// <summary>
        ///update color
        /// </summary>
        /// <param name="request">Update color request</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostUpdateColorAsync([FromBody] UpdateColorRequestDto request)
        {
            try
            {
                // debugging information
                System.Diagnostics.Debug.WriteLine($"UpdateColor API Called - Id: {request.Id}, Red: {request.Red}, Green: {request.Green}, Blue: {request.Blue}, AdditionNote: {request.AdditionNote}");
                
                if (request == null)
                {
                    return Ok(new { success = false, message = "请求参数不能为空" });
                }
                
                var color = await _colorService.GetObjectAsync(c => c.Id == request.Id);
                if (color == null)
                {
                    return Ok(new { success = false, message = "颜色不存在" });
                }

                // renew
                color.Update(request);
                
                await _colorService.SaveObjectAsync(color);
                
                return Ok(new { success = true, message = "颜色更新成功", data = new { color.Id, color.Red, color.Green, color.Blue, color.AdditionNote, color.LastUpdateTime } });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateColor API Error: {ex.Message}");
                return Ok(new { success = false, message = "更新失败：" + ex.Message });
            }
        }

        /// <summary>
        ///delete color
        /// </summary>
        /// <param name="request">Delete color request</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostDeleteColorAsync([FromBody] DeleteColorRequestDto request)
        {
            try
            {
                // debugging information
                System.Diagnostics.Debug.WriteLine($"DeleteColor API Called - Id: {request.Id}");
                
                if (request == null)
                {
                    return Ok(new { success = false, message = "请求参数不能为空" });
                }
                
                var color = await _colorService.GetObjectAsync(c => c.Id == request.Id);
                if (color == null)
                {
                    return Ok(new { success = false, message = "颜色不存在" });
                }

                await _colorService.DeleteObjectAsync(color);
                return Ok(new { success = true, message = "颜色删除成功" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteColor API Error: {ex.Message}");
                return Ok(new { success = false, message = "删除失败：" + ex.Message });
            }
        }

        /// <summary>
        /// Randomize the specified color
        /// </summary>
        /// <param name="request">Randomize color request</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostRandomizeColorAsync([FromBody] RandomizeColorRequestDto request)
        {
            try
            {
                // debugging information
                System.Diagnostics.Debug.WriteLine($"RandomizeColor API Called - Id: {request.Id}");
                
                if (request == null)
                {
                    return Ok(new { success = false, message = "请求参数不能为空" });
                }
                
                var color = await _colorService.GetObjectAsync(c => c.Id == request.Id);
                if (color == null)
                {
                    return Ok(new { success = false, message = "颜色不存在" });
                }

                color.Random();
                await _colorService.SaveObjectAsync(color);
                
                return Ok(new { success = true, message = "颜色随机化成功", data = new { color.Id, color.Red, color.Green, color.Blue, color.AdditionNote, color.LastUpdateTime } });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RandomizeColor API Error: {ex.Message}");
                return Ok(new { success = false, message = "随机化失败：" + ex.Message });
            }
        }

        /// <summary>
        /// Get color details
        /// </summary>
        /// <param name="id">Color ID</param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetColorDetailAsync(int id)
        {
            try
            {
                var color = await _colorService.GetObjectAsync(c => c.Id == id);
                if (color == null)
                {
                    return Ok(new { success = false, message = "颜色不存在" });
                }

                return Ok(new { success = true, data = new { color.Id, color.Red, color.Green, color.Blue, color.AdditionNote, color.AddTime, color.LastUpdateTime, color.Remark } });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "获取失败：" + ex.Message });
            }
        }
    }
} 