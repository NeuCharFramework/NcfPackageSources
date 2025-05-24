using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using Template_OrgName.Xncf.Template_XncfName.Domain.Services;
using Template_OrgName.Xncf.Template_XncfName.Models.DatabaseModel.Dto;
using System;
using System.Linq;
using System.Threading.Tasks;

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
        /// 获取颜色列表（分页）
        /// </summary>
        /// <param name="keyword">关键词</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetColorListAsync(string keyword, string orderField, int pageIndex, int pageSize)
        {
            var seh = new SenparcExpressionHelper<Color>();
            // 可以根据需要添加搜索条件
            // seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(keyword), _ => _.Remark.Contains(keyword));
            var where = seh.BuildWhereExpression();
            var response = await _colorService.GetObjectListAsync(pageIndex, pageSize, where, orderField ?? "Id desc");
            
            return Ok(new
            {
                totalCount = response.TotalCount,
                pageIndex = response.PageIndex,
                list = response.Select(_ => new
                {
                    id = _.Id,
                    red = _.Red,
                    green = _.Green,
                    blue = _.Blue,
                    addTime = _.AddTime,
                    lastUpdateTime = _.LastUpdateTime,
                    remark = _.Remark
                })
            });
        }

        /// <summary>
        /// 创建新颜色
        /// </summary>
        /// <param name="red">红色值</param>
        /// <param name="green">绿色值</param>
        /// <param name="blue">蓝色值</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostCreateColorAsync(int red, int green, int blue)
        {
            try
            {
                var color = new Color(red, green, blue);
                await _colorService.SaveObjectAsync(color);
                
                return Ok(new { success = true, message = "颜色创建成功", data = new { color.Id, color.Red, color.Green, color.Blue, color.AddTime, color.LastUpdateTime } });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "创建失败：" + ex.Message });
            }
        }

        /// <summary>
        /// 更新颜色
        /// </summary>
        /// <param name="id">颜色ID</param>
        /// <param name="red">红色值</param>
        /// <param name="green">绿色值</param>
        /// <param name="blue">蓝色值</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostUpdateColorAsync(int id, int red, int green, int blue)
        {
            try
            {
                var color = await _colorService.GetObjectAsync(c => c.Id == id);
                if (color == null)
                {
                    return Ok(new { success = false, message = "颜色不存在" });
                }

                // 直接修改现有对象的属性值
                color.Red = red;
                color.Green = green;
                color.Blue = blue;
                
                await _colorService.SaveObjectAsync(color);
                
                return Ok(new { success = true, message = "颜色更新成功", data = new { color.Id, color.Red, color.Green, color.Blue, color.LastUpdateTime } });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "更新失败：" + ex.Message });
            }
        }

        /// <summary>
        /// 删除颜色
        /// </summary>
        /// <param name="id">颜色ID</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostDeleteColorAsync(int id)
        {
            try
            {
                var color = await _colorService.GetObjectAsync(c => c.Id == id);
                if (color == null)
                {
                    return Ok(new { success = false, message = "颜色不存在" });
                }

                await _colorService.DeleteObjectAsync(color);
                return Ok(new { success = true, message = "颜色删除成功" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "删除失败：" + ex.Message });
            }
        }

        /// <summary>
        /// 随机化指定颜色
        /// </summary>
        /// <param name="id">颜色ID</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostRandomizeColorAsync(int id)
        {
            try
            {
                var color = await _colorService.GetObjectAsync(c => c.Id == id);
                if (color == null)
                {
                    return Ok(new { success = false, message = "颜色不存在" });
                }

                color.Random();
                await _colorService.SaveObjectAsync(color);
                
                return Ok(new { success = true, message = "颜色随机化成功", data = new { color.Id, color.Red, color.Green, color.Blue, color.LastUpdateTime } });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "随机化失败：" + ex.Message });
            }
        }

        /// <summary>
        /// 获取颜色详情
        /// </summary>
        /// <param name="id">颜色ID</param>
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

                return Ok(new { success = true, data = new { color.Id, color.Red, color.Green, color.Blue, color.AddTime, color.LastUpdateTime, color.Remark } });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "获取失败：" + ex.Message });
            }
        }
    }
} 