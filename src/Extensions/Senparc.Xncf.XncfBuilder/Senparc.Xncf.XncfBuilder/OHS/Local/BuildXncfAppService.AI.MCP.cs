using ModelContextProtocol.Server;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.XncfBuilder.OHS.PL;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.CO2NET.Extensions;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Senparc.AI.Entities.Keys;

namespace Senparc.Xncf.XncfBuilder.OHS.Local
{
    /// <summary>
    /// MCP Server Tools
    /// </summary>
    public partial class BuildXncfAppService
    {
        private string GetFilePath(string moduleName, string filePath)
        {
            BuildXncf_BuildRequest request = new BuildXncf_BuildRequest();
            var slnPath = request.GetSlnFilePath();
            var modulePath = Directory.GetDirectories(Path.GetDirectoryName(slnPath), moduleName, SearchOption.AllDirectories)
                                    .FirstOrDefault();

            if (string.IsNullOrEmpty(modulePath))
            {
                throw new Exception($"未找到模块 {moduleName} 的目录，请检查模块名称是否完整，必须完全匹配，如：Senparc.Xncf.XncfBuilder。");
            }

            var fullFilePath = Path.Combine(modulePath, filePath);
            return fullFilePath;
        }


        #region MCP AI 接入（由于官方组件 bug，暂时使用平铺参数方式接入）

        [McpServerTool, Description("生成 XNCF 模块")]
        //[FunctionRender("生成 XNCF", "根据配置条件生成 XNCF", typeof(Register))]
        public async Task<StringAppResponse> Build(
            // [Required,Description("解决方案文件路径")]
            // string slnFilePath, 
            [Description("组织名称，默认为 Senparc")]
            string orgName,
            [Required, Description("模块名称")]
            string xncfName,
            [Required, Description("版本号，默认为 1.0.0")]
            string version,
            [Required, Description("菜单显示名称")]
            string menuName,
            [Required, Description("图标，支持 Font Awesome 图标集")]
            string icon,
            [Description("模块说明")]
            string description)
        {
            Console.WriteLine("XNCF Builder: Receive MCP Call");

            BuildXncf_BuildRequest request = new BuildXncf_BuildRequest()
            {
                //   SlnFilePath = slnFilePath,
                OrgName = orgName,
                XncfName = xncfName,
                Version = version,
                MenuName = menuName,
                Icon = icon,
                Description = description,
                UseSammple = new Ncf.XncfBase.Functions.SelectionList(Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
                new Ncf.XncfBase.Functions.SelectionItem("1","使用示例","使用示例",true),
              }),
                UseModule = new Ncf.XncfBase.Functions.SelectionList(Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
                new Ncf.XncfBase.Functions.SelectionItem("database","数据库","使用数据库",true),
              }),
                //   UseWeb = new Ncf.XncfBase.Functions.SelectionList( Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
                //     new Ncf.XncfBase.Functions.SelectionItem("1","使用Web","使用Web",true),
                //   }),
                //   UseWebApi = new Ncf.XncfBase.Functions.SelectionList( Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
                //     new Ncf.XncfBase.Functions.SelectionItem("1","使用WebApi","使用WebApi",true),
                //   }),
                NewSlnFile = new Ncf.XncfBase.Functions.SelectionList(Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
                new Ncf.XncfBase.Functions.SelectionItem("backup","备份 .sln 文件（推荐）","如果使用覆盖现有 .sln 文件，对当前文件进行备份",true),
              }),
                TemplatePackage = new Ncf.XncfBase.Functions.SelectionList(Ncf.XncfBase.Functions.SelectionType.DropDownList, new[] {
                new Ncf.XncfBase.Functions.SelectionItem("no","已安装，不需要安装新版本","请确保已经在本地安装过版本（无论新旧），否则将自动从在线获取",true),
              }),
                FrameworkVersion = new Ncf.XncfBase.Functions.SelectionList(Ncf.XncfBase.Functions.SelectionType.DropDownList, new[] {
                new Ncf.XncfBase.Functions.SelectionItem("net8.0","net8.0","使用 .NET 8.0",false),
              })
            };

            request.SlnFilePath = request.GetSlnFilePath();
            request.UseSammple.SelectedValues = new[] { "1" };
            request.UseModule.SelectedValues = new[] { "database" };
            request.NewSlnFile.SelectedValues = new[] { "backup" };
            request.TemplatePackage.SelectedValues = new[] { "no" };
            request.FrameworkVersion.SelectedValues = new[] { "net8.0" };

            Console.WriteLine("XNCF Builder parameters:" + request.ToJson(true));

            return await this.Build(request);
        }

        #endregion

        [McpServerTool, Description("获取前端代码模板示例")]
        public async Task<string> GetFrontEndCodeTemplate()
        {
            var template = @"
## Page UI sample (front-end)
File Name: DatabaseSampleIndex.cshtml
File Path: <ModuleRootPath>/Areas/Admin/Pages/Template_XncfName
Code:
```razorpage
@@page
@@model Template_OrgName.Xncf.Template_XncfName.Areas.Template_XncfName.Pages.DatabaseSampleIndex
@@{
    ViewData[""Title""] = ""Color 数据库管理"";
    Layout = ""_Layout_Vue"";
}

@@section Style {
    <link href=""~/css/Admin/Template_XncfName/databaseSampleIndex.css"" rel=""stylesheet"" />
}

@@section breadcrumbs {
    <el-breadcrumb-item>扩展模块</el-breadcrumb-item>
    <el-breadcrumb-item>Template_MenuName</el-breadcrumb-item>
    <el-breadcrumb-item>Color 数据库管理</el-breadcrumb-item>
}

<div>
    <div class=""filter-container"">
        <el-button type=""primary"" icon=""el-icon-plus"" size=""small"" @@@@click=""addColor"">添加颜色</el-button>
        <el-button type=""success"" icon=""el-icon-refresh"" size=""small"" @@@@click=""refreshList"">刷新</el-button>
        <el-button type=""info"" icon=""el-icon-info"" size=""small"" @@@@click=""debugInfo"">调试信息</el-button>
    </div>

    <!-- 调试信息显示 -->
    <div v-if=""showDebug"" style=""background: #f0f9ff; border: 1px solid #0ea5e9; padding: 10px; margin: 10px 0; border-radius: 4px;"">
        <h4>调试信息：</h4>
        <p><strong>tableData长度:</strong> {{ tableData ? tableData.length : 'null/undefined' }}</p>
        <p><strong>total:</strong> {{ total }}</p>
        <p><strong>tableLoading:</strong> {{ tableLoading }}</p>
        <p><strong>Vue实例是否正常:</strong> {{ $el ? '是' : '否' }}</p>
        <div v-if=""tableData && tableData.length > 0"">
            <strong>第一条数据:</strong>
            <pre>{{ JSON.stringify(tableData[0], null, 2) }}</pre>
        </div>
        
        <!-- 简化数据显示测试 -->
        <div style=""background: #fff3cd; border: 1px solid #ffc107; padding: 10px; margin: 10px 0; border-radius: 4px;"">
            <h5>简化数据显示测试:</h5>
            <div v-for=""(item, index) in tableData"" :key=""item.id || index"" style=""border: 1px solid #ccc; margin: 5px 0; padding: 5px;"">
                <strong>ID:</strong> {{item.id}} | 
                <strong>RGB:</strong> {{item.red}},{{item.green}},{{item.blue}} | 
                <strong>时间:</strong> {{item.addTime}}
            </div>
            <p v-if=""!tableData || tableData.length === 0"" style=""color: red;"">
                <strong>没有数据显示！</strong>
            </p>
        </div>
    </div>

    <el-table :data=""tableData"" v-loading=""tableLoading"" border>
        <el-table-column prop=""id"" label=""ID"" width=""auto""></el-table-column>
        <el-table-column prop=""red"" label=""红色值 (R)"" width=""auto"">
            <template slot-scope=""scope"">
                <el-tag :style=""{backgroundColor: `rgb(${scope.row.red}, 0, 0)`, color: 'white'}"">
                    {{scope.row.red}}
                </el-tag>
            </template>
        </el-table-column>
        <el-table-column prop=""green"" label=""绿色值 (G)"" width=""auto"">
            <template slot-scope=""scope"">
                <el-tag :style=""{backgroundColor: `rgb(0, ${scope.row.green}, 0)`, color: 'white'}"">
                    {{scope.row.green}}
                </el-tag>
            </template>
        </el-table-column>
        <el-table-column prop=""blue"" label=""蓝色值 (B)"" width=""auto"">
            <template slot-scope=""scope"">
                <el-tag :style=""{backgroundColor: `rgb(0, 0, ${scope.row.blue})`, color: 'white'}"">
                    {{scope.row.blue}}
                </el-tag>
            </template>
        </el-table-column>
        <el-table-column label=""颜色预览"" width=""auto"">
            <template slot-scope=""scope"">
                <div class=""color-preview"" :style=""{backgroundColor: `rgb(${scope.row.red}, ${scope.row.green}, ${scope.row.blue})`}"">
                    RGB({{scope.row.red}}, {{scope.row.green}}, {{scope.row.blue}})
                </div>
            </template>
        </el-table-column>
        <el-table-column prop=""addTime"" label=""创建时间"" width=""auto"">
            <template slot-scope=""scope"">
                {{ dateformatter(scope.row.addTime) }}
            </template>
        </el-table-column>
        <el-table-column prop=""lastUpdateTime"" label=""更新时间"" width=""auto"">
            <template slot-scope=""scope"">
                {{ dateformatter(scope.row.lastUpdateTime) }}
            </template>
        </el-table-column>
        <el-table-column label=""操作"" width=""auto"">
            <template slot-scope=""scope"">
                <el-button type=""primary"" size=""mini"" @@@@click=""editColor(scope.row)"">编辑</el-button>
                <el-button type=""warning"" size=""mini"" @@@@click=""randomizeColor(scope.row)"">随机</el-button>
                    <el-button type=""danger"" size=""mini"" @@@@click=""deleteColor(scope.row)"">删除</el-button>
            </template>
        </el-table-column>
    </el-table>
    
    <div class=""pagination-container"">
        <el-pagination @@@@current-change=""handleCurrentChange""
                       @@@@size-change=""handleSizeChange""
                       :current-page=""page.page""
                       :page-sizes=""[10, 20, 30, 40]""
                       :page-size=""page.size""
                       layout=""sizes, prev, next, jumper""
                       :total=""total""
                       background
                       style=""margin-top: 20px"">
        </el-pagination>
    </div>

    @@* dialog for 添加颜色 *@@
    <el-dialog title=""添加颜色"" :visible.sync=""addFormDialogVisible"" width=""50%"" :close-on-click-modal=""false"">
        <el-form :model=""addForm"" label-width=""120px"" :rules=""addRules"" ref=""addForm"">
            <el-form-item label=""红色值 (R)"" prop=""red"">
                <el-slider v-model=""addForm.red"" :min=""0"" :max=""255"" show-input></el-slider>
            </el-form-item>
            <el-form-item label=""绿色值 (G)"" prop=""green"">
                <el-slider v-model=""addForm.green"" :min=""0"" :max=""255"" show-input></el-slider>
            </el-form-item>
            <el-form-item label=""蓝色值 (B)"" prop=""blue"">
                <el-slider v-model=""addForm.blue"" :min=""0"" :max=""255"" show-input></el-slider>
            </el-form-item>
            <el-form-item label=""颜色预览"">
                <div class=""color-preview-large"" :style=""{backgroundColor: `rgb(${addForm.red}, ${addForm.green}, ${addForm.blue})`}"">
                    RGB({{addForm.red}}, {{addForm.green}}, {{addForm.blue}})
                </div>
            </el-form-item>
            <el-form-item label=""附加备注"" prop=""additionNote"">
                <el-input v-model=""addForm.additionNote"" type=""textarea"" :rows=""3"" placeholder=""请输入颜色的附加备注信息...""></el-input>
            </el-form-item>
            <el-form-item>
                <el-button @@@@click=""randomizeForm"" type=""info"">随机颜色</el-button>
            </el-form-item>
        </el-form>
        <span slot=""footer"" class=""dialog-footer"">
            <el-button @@@@click=""addFormDialogVisible = false"">取 消</el-button>
            <el-button type=""primary"" @@@@click=""addColorSubmit"">确 定</el-button>
        </span>
    </el-dialog>

    @@* dialog for 编辑颜色 *@@
    <el-dialog title=""编辑颜色"" :visible.sync=""editFormDialogVisible"" width=""50%"" :close-on-click-modal=""false"">
        <el-form :model=""editForm"" label-width=""120px"" :rules=""editRules"" ref=""editForm"">
            <el-form-item label=""红色值 (R)"" prop=""red"">
                <el-slider v-model=""editForm.red"" :min=""0"" :max=""255"" show-input></el-slider>
            </el-form-item>
            <el-form-item label=""绿色值 (G)"" prop=""green"">
                <el-slider v-model=""editForm.green"" :min=""0"" :max=""255"" show-input></el-slider>
            </el-form-item>
            <el-form-item label=""蓝色值 (B)"" prop=""blue"">
                <el-slider v-model=""editForm.blue"" :min=""0"" :max=""255"" show-input></el-slider>
            </el-form-item>
            <el-form-item label=""颜色预览"">
                <div class=""color-preview-large"" :style=""{backgroundColor: `rgb(${editForm.red}, ${editForm.green}, ${editForm.blue})`}"">
                    RGB({{editForm.red}}, {{editForm.green}}, {{editForm.blue}})
                </div>
            </el-form-item>
            <el-form-item label=""附加备注"" prop=""additionNote"">
                <el-input v-model=""editForm.additionNote"" type=""textarea"" :rows=""3"" placeholder=""请输入颜色的附加备注信息...""></el-input>
            </el-form-item>
            <el-form-item>
                <el-button @@@@click=""randomizeEditForm"" type=""info"">随机颜色</el-button>
            </el-form-item>
        </el-form>
        <span slot=""footer"" class=""dialog-footer"">
            <el-button @@@@click=""editFormDialogVisible = false"">取 消</el-button>
            <el-button type=""primary"" @@@@click=""editColorSubmit"">确 定</el-button>
        </span>
    </el-dialog>
</div>

@@section scripts{
    <script src=""~/js/Admin/Template_XncfName/databaseSampleIndex.js""></script>
} 
```

## Page UI sample (back-end)
File Name: DatabaseSampleIndex.cshtml.cs
File Path: <ModuleRootPath>/Areas/Admin/Pages/Template_XncfName
Code:
```csharp
using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using Template_OrgName.Xncf.Template_XncfName.Domain.Services;
using Template_OrgName.Xncf.Template_XncfName.Domain.Models.DatabaseModel.Dto;
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
        /// <param name=""keyword"">关键词</param>
        /// <param name=""orderField"">排序字段</param>
        /// <param name=""pageIndex"">页码</param>
        /// <param name=""pageSize"">页大小</param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetColorListAsync(string keyword, string orderField, int pageIndex, int pageSize)
        {
            try
            {
                // 调试信息
                System.Diagnostics.Debug.WriteLine($""ColorList API Called - PageIndex: {pageIndex}, PageSize: {pageSize}, OrderField: {orderField}"");
                
                var seh = new SenparcExpressionHelper<Color>();
                // 可以根据需要添加搜索条件
                // seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(keyword), _ => _.Remark.Contains(keyword));
                var where = seh.BuildWhereExpression();
                var response = await _colorService.GetObjectListAsync(pageIndex, pageSize, where, orderField ?? ""Id desc"");
                
                // 调试信息
                System.Diagnostics.Debug.WriteLine($""Database Query Result - TotalCount: {response.TotalCount}, ItemCount: {response.Count()}"");
                
                var result = new
                {
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
                };
                
                // 调试信息
                System.Diagnostics.Debug.WriteLine($""API Response - ListCount: {result.list.Count}"");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($""ColorList API Error: {ex.Message}"");
                return Ok(new { 
                    success = false, 
                    message = ""获取数据失败: "" + ex.Message,
                    totalCount = 0,
                    pageIndex = pageIndex,
                    list = new object[0]
                });
            }
        }

        /// <summary>
        /// 创建新颜色
        /// </summary>
        /// <param name=""request"">创建颜色请求</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostCreateColorAsync([FromBody] CreateColorRequestDto request)
        {
            try
            {
                // 调试信息
                System.Diagnostics.Debug.WriteLine($""CreateColor API Called - Red: {request.Red}, Green: {request.Green}, Blue: {request.Blue}, AdditionNote: {request.AdditionNote}"");
                
                if (request == null)
                {
                    return Ok(new { success = false, message = ""请求参数不能为空"" });
                }
                
                var color = new Color(request.Red, request.Green, request.Blue, request.AdditionNote);
                await _colorService.SaveObjectAsync(color);
                
                return Ok(new { success = true, message = ""颜色创建成功"", data = new { color.Id, color.Red, color.Green, color.Blue, color.AdditionNote, color.AddTime, color.LastUpdateTime } });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($""CreateColor API Error: {ex.Message}"");
                return Ok(new { success = false, message = ""创建失败："" + ex.Message });
            }
        }

        /// <summary>
        /// 更新颜色
        /// </summary>
        /// <param name=""request"">更新颜色请求</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostUpdateColorAsync([FromBody] UpdateColorRequestDto request)
        {
            try
            {
                // 调试信息
                System.Diagnostics.Debug.WriteLine($""UpdateColor API Called - Id: {request.Id}, Red: {request.Red}, Green: {request.Green}, Blue: {request.Blue}, AdditionNote: {request.AdditionNote}"");
                
                if (request == null)
                {
                    return Ok(new { success = false, message = ""请求参数不能为空"" });
                }
                
                var color = await _colorService.GetObjectAsync(c => c.Id == request.Id);
                if (color == null)
                {
                    return Ok(new { success = false, message = ""颜色不存在"" });
                }

                // 直接修改现有对象的属性值
                color.Red = request.Red;
                color.Green = request.Green;
                color.Blue = request.Blue;
                color.AdditionNote = request.AdditionNote;
                
                await _colorService.SaveObjectAsync(color);
                
                return Ok(new { success = true, message = ""颜色更新成功"", data = new { color.Id, color.Red, color.Green, color.Blue, color.AdditionNote, color.LastUpdateTime } });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($""UpdateColor API Error: {ex.Message}"");
                return Ok(new { success = false, message = ""更新失败："" + ex.Message });
            }
        }

        /// <summary>
        /// 删除颜色
        /// </summary>
        /// <param name=""request"">删除颜色请求</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostDeleteColorAsync([FromBody] DeleteColorRequestDto request)
        {
            try
            {
                // 调试信息
                System.Diagnostics.Debug.WriteLine($""DeleteColor API Called - Id: {request.Id}"");
                
                if (request == null)
                {
                    return Ok(new { success = false, message = ""请求参数不能为空"" });
                }
                
                var color = await _colorService.GetObjectAsync(c => c.Id == request.Id);
                if (color == null)
                {
                    return Ok(new { success = false, message = ""颜色不存在"" });
                }

                await _colorService.DeleteObjectAsync(color);
                return Ok(new { success = true, message = ""颜色删除成功"" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($""DeleteColor API Error: {ex.Message}"");
                return Ok(new { success = false, message = ""删除失败："" + ex.Message });
            }
        }

        /// <summary>
        /// 随机化指定颜色
        /// </summary>
        /// <param name=""request"">随机化颜色请求</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostRandomizeColorAsync([FromBody] RandomizeColorRequestDto request)
        {
            try
            {
                // 调试信息
                System.Diagnostics.Debug.WriteLine($""RandomizeColor API Called - Id: {request.Id}"");
                
                if (request == null)
                {
                    return Ok(new { success = false, message = ""请求参数不能为空"" });
                }
                
                var color = await _colorService.GetObjectAsync(c => c.Id == request.Id);
                if (color == null)
                {
                    return Ok(new { success = false, message = ""颜色不存在"" });
                }

                color.Random();
                await _colorService.SaveObjectAsync(color);
                
                return Ok(new { success = true, message = ""颜色随机化成功"", data = new { color.Id, color.Red, color.Green, color.Blue, color.AdditionNote, color.LastUpdateTime } });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($""RandomizeColor API Error: {ex.Message}"");
                return Ok(new { success = false, message = ""随机化失败："" + ex.Message });
            }
        }

        /// <summary>
        /// 获取颜色详情
        /// </summary>
        /// <param name=""id"">颜色ID</param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetColorDetailAsync(int id)
        {
            try
            {
                var color = await _colorService.GetObjectAsync(c => c.Id == id);
                if (color == null)
                {
                    return Ok(new { success = false, message = ""颜色不存在"" });
                }

                return Ok(new { success = true, data = new { color.Id, color.Red, color.Green, color.Blue, color.AdditionNote, color.AddTime, color.LastUpdateTime, color.Remark } });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ""获取失败："" + ex.Message });
            }
        }
    }
} 
```

## Page JavaScript file sample
File Name: databaseSampleIndex.js
File Path: <ModuleRootPath>/wwwroot/js/Admin/Template_XncfName
Code:
```javascript
var app = new Vue({
    el: ""#app"",
    data() {
        return {
            page: {
                page: 1,
                size: 10
            },
            tableLoading: true,
            tableData: [],
            showDebug: false,
            addFormDialogVisible: false,
            addForm: {
                red: 128,
                green: 128,
                blue: 128,
                additionNote: ''
            },
            editFormDialogVisible: false,
            editForm: {
                id: 0,
                red: 128,
                green: 128,
                blue: 128,
                additionNote: ''
            },
            total: 0,
            addRules: {
                red: [
                    { required: true, message: '请设置红色值', trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: '红色值范围为0-255', trigger: 'change' }
                ],
                green: [
                    { required: true, message: '请设置绿色值', trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: '绿色值范围为0-255', trigger: 'change' }
                ],
                blue: [
                    { required: true, message: '请设置蓝色值', trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: '蓝色值范围为0-255', trigger: 'change' }
                ]
            },
            editRules: {
                red: [
                    { required: true, message: '请设置红色值', trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: '红色值范围为0-255', trigger: 'change' }
                ],
                green: [
                    { required: true, message: '请设置绿色值', trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: '绿色值范围为0-255', trigger: 'change' }
                ],
                blue: [
                    { required: true, message: '请设置蓝色值', trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: '蓝色值范围为0-255', trigger: 'change' }
                ]
            }
        }
    },
    mounted() {
        //wait page load  
        setTimeout(async () => {
            await this.init();
        }, 100)
    },
    methods: {
        async init() {
            await this.getDataList();
        },
        async handleSizeChange(val) {
            this.page.size = val;
            await this.getDataList();
        },
        async handleCurrentChange(val) {
            this.page.page = val;
            await this.getDataList();
        },
        async getDataList() {
            this.tableLoading = true
            await service.get('/Admin/Template_XncfName/DatabaseSampleIndex?handler=ColorList', {
                params: {
                    pageIndex: this.page.page,
                    pageSize: this.page.size,
                    orderField: ""Id desc"",
                    keyword: """"
                }
            })
                .then(res => {
                    console.log('=== API Response Debug ===');
                    console.log('Complete Response:', res);
                    console.log('Response Data:', res.data);
                    console.log('Response Data Type:', typeof res.data);
                    console.log('Has res.data.data?:', res.data && res.data.data);
                    console.log('Has res.data.data.list?:', res.data && res.data.data && res.data.data.list);
                    console.log('res.data.data.list value:', res.data && res.data.data ? res.data.data.list : 'nested data not found');
                    console.log('==================');
                    
                    // 尝试多种可能的数据结构
                    let dataList = null;
                    let totalCount = 0;
                    let dataSource = '';
                    
                    if (res.data && res.data.data && res.data.data.list) {
                        // NCF框架标准格式: {data: {data: {list, totalCount}}}
                        dataList = res.data.data.list;
                        totalCount = res.data.data.totalCount || 0;
                        dataSource = 'NCF标准格式: res.data.data.list';
                        console.log('✅ 使用NCF标准格式: res.data.data.list');
                        console.log('✅ List数据:', dataList);
                        console.log('✅ TotalCount:', totalCount);
                    } else if (res.data && res.data.list) {
                        // 简单格式: {data: {list, totalCount}}
                        dataList = res.data.list;
                        totalCount = res.data.totalCount || 0;
                        dataSource = '简单格式: res.data.list';
                        console.log('✅ 使用简单格式: res.data.list');
                    } else if (res.data && Array.isArray(res.data)) {
                        // 如果data直接是数组
                        dataList = res.data;
                        totalCount = res.data.length;
                        dataSource = '数组格式: res.data (array)';
                        console.log('✅ 使用数组格式: res.data (array)');
                    } else if (res && res.list) {
                        // 如果list在顶层
                        dataList = res.list;
                        totalCount = res.totalCount || 0;
                        dataSource = '顶层格式: res.list';
                        console.log('✅ 使用顶层格式: res.list');
                    } else {
                        console.error('❌ 无法识别的数据格式:', res);
                        console.log('🔍 尝试的路径:');
                        console.log('- res.data.data.list:', res.data && res.data.data ? res.data.data.list : 'not found');
                        console.log('- res.data.list:', res.data ? res.data.list : 'not found');
                        console.log('- res.data (array):', res.data && Array.isArray(res.data) ? 'is array' : 'not array');
                        console.log('- res.list:', res.list ? res.list : 'not found');
                        dataList = [];
                        totalCount = 0;
                        dataSource = '无法识别格式';
                    }
                    
                    console.log('🎯 Final dataList:', dataList);
                    console.log('🎯 Final totalCount:', totalCount);
                    console.log('🎯 Data source:', dataSource);
                    
                    // 数据赋值前的状态
                    console.log('📋 赋值前 tableData:', this.tableData);
                    console.log('📋 赋值前 total:', this.total);
                    
                    this.tableData = dataList || [];
                    this.total = totalCount;
                    
                    // 数据赋值后的状态
                    console.log('📋 赋值后 tableData:', this.tableData);
                    console.log('📋 赋值后 tableData.length:', this.tableData.length);
                    console.log('📋 赋值后 total:', this.total);
                    
                    // 强制Vue更新
                    this.$forceUpdate();
                    console.log('🔄 Vue已强制更新');
                    
                    // 延迟检查数据是否正确绑定
                    setTimeout(() => {
                        console.log('⏰ 延迟检查 tableData:', this.tableData);
                        console.log('⏰ 延迟检查 tableData.length:', this.tableData ? this.tableData.length : 'null');
                    }, 100);
                    
                    this.tableLoading = false
                })
                .catch(error => {
                    console.error('获取数据失败:', error);
                    this.tableLoading = false;
                    this.$message.error('获取数据失败: ' + (error.message || error));
                });
        },
        addColor() {
            this.addFormDialogVisible = true;
        },
        refreshList() {
            this.getDataList();
        },
        async addColorSubmit() {
            this.$refs.addForm.validate(async (valid) => {
                if (valid) {
                    console.log('📤 发送创建请求:', {
                        red: this.addForm.red,
                        green: this.addForm.green,
                        blue: this.addForm.blue,
                        additionNote: this.addForm.additionNote
                    });
                    
                    await service.post('/Admin/Template_XncfName/DatabaseSampleIndex?handler=CreateColor', {
                        red: this.addForm.red,
                        green: this.addForm.green,
                        blue: this.addForm.blue,
                        additionNote: this.addForm.additionNote
                    }, {
                        headers: {
                            'Content-Type': 'application/json'
                        }
                    })
                        .then(res => {
                            console.log('📥 创建响应:', res);
                            this.$message({
                                type: res.data.success ? 'success' : 'error',
                                message: res.data.message
                            });
                            if (res.data.success) {
                                this.getDataList()
                                this.clearAddForm()
                                this.addFormDialogVisible = false;
                            }
                        })
                        .catch(error => {
                            console.error('创建失败:', error);
                            this.$message.error('创建失败');
                        });
                } else {
                    return false;
                }
            });
        },
        clearAddForm() {
            this.addForm = {
                red: 128,
                green: 128,
                blue: 128,
                additionNote: ''
            };
            if (this.$refs.addForm) {
                this.$refs.addForm.resetFields();
            }
        },
        clearEditForm() {
            this.editForm = {
                id: 0,
                red: 128,
                green: 128,
                blue: 128,
                additionNote: ''
            };
            if (this.$refs.editForm) {
                this.$refs.editForm.resetFields();
            }
        },
        async editColorSubmit() {
            this.$refs.editForm.validate(async (valid) => {
                if (valid) {
                    console.log('📤 发送更新请求:', {
                        id: this.editForm.id,
                        red: this.editForm.red,
                        green: this.editForm.green,
                        blue: this.editForm.blue,
                        additionNote: this.editForm.additionNote
                    });
                    
                    await service.post('/Admin/Template_XncfName/DatabaseSampleIndex?handler=UpdateColor', {
                        id: this.editForm.id,
                        red: this.editForm.red,
                        green: this.editForm.green,
                        blue: this.editForm.blue,
                        additionNote: this.editForm.additionNote
                    }, {
                        headers: {
                            'Content-Type': 'application/json'
                        }
                    })
                        .then(res => {
                            console.log('📥 更新响应:', res);
                            this.$message({
                                type: res.data.success ? 'success' : 'error',
                                message: res.data.message
                            });
                            if (res.data.success) {
                                this.getDataList()
                                this.clearEditForm()
                                this.editFormDialogVisible = false;
                            }
                        })
                        .catch(error => {
                            console.error('更新失败:', error);
                            this.$message.error('更新失败');
                        });
                } else {
                    return false;
                }
            });
        },
        dateformatter(date) {
            if (!date) return '';
            
            try {
                // 使用原生JavaScript格式化日期
                const d = new Date(date);
                
                // 检查日期是否有效
                if (isNaN(d.getTime())) {
                    return date; // 如果无法解析，返回原始值
                }
                
                // 格式化为 YYYY-MM-DD HH:mm:ss
                const year = d.getFullYear();
                const month = String(d.getMonth() + 1).padStart(2, '0');
                const day = String(d.getDate()).padStart(2, '0');
                const hours = String(d.getHours()).padStart(2, '0');
                const minutes = String(d.getMinutes()).padStart(2, '0');
                const seconds = String(d.getSeconds()).padStart(2, '0');
                
                return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
            } catch (error) {
                console.warn('日期格式化错误:', error, '原始值:', date);
                return date; // 如果格式化失败，返回原始值
            }
        },
        editColor(row) {
            this.editForm = {
                id: row.id,
                red: row.red,
                green: row.green,
                blue: row.blue,
                additionNote: row.additionNote || ''
            };
            this.editFormDialogVisible = true;
        },
        deleteColor(row) {
            this.$confirm('此操作将永久删除该颜色, 是否继续?', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }).then(async () => {
                console.log('📤 发送删除请求:', { id: row.id });
                
                await service.post('/Admin/Template_XncfName/DatabaseSampleIndex?handler=DeleteColor', {
                    id: row.id
                }, {
                    headers: {
                        'Content-Type': 'application/json'
                    }
                })
                    .then(res => {
                        console.log('📥 删除响应:', res);
                        this.$message({
                            type: res.data.success ? 'success' : 'error',
                            message: res.data.message
                        });
                        if (res.data.success) {
                            this.getDataList();
                        }
                    })
                    .catch(error => {
                        console.error('删除失败:', error);
                        this.$message.error('删除失败');
                    });
            }).catch(() => {
                this.$message({
                    type: 'info',
                    message: '已取消删除'
                });
            });
        },
        async randomizeColor(row) {
            console.log('📤 发送随机化请求:', { id: row.id });
            
            await service.post('/Admin/Template_XncfName/DatabaseSampleIndex?handler=RandomizeColor', {
                id: row.id
            }, {
                headers: {
                    'Content-Type': 'application/json'
                }
            })
                .then(res => {
                    console.log('📥 随机化响应:', res);
                    this.$message({
                        type: res.data.success ? 'success' : 'error',
                        message: res.data.message
                    });
                    if (res.data.success) {
                        this.getDataList();
                    }
                })
                .catch(error => {
                    console.error('随机化失败:', error);
                    this.$message.error('随机化失败');
                });
        },
        randomizeForm() {
            this.addForm.red = Math.floor(Math.random() * 256);
            this.addForm.green = Math.floor(Math.random() * 256);
            this.addForm.blue = Math.floor(Math.random() * 256);
        },
        randomizeEditForm() {
            this.editForm.red = Math.floor(Math.random() * 256);
            this.editForm.green = Math.floor(Math.random() * 256);
            this.editForm.blue = Math.floor(Math.random() * 256);
        },
        debugInfo() {
            this.showDebug = !this.showDebug;
            console.log('=== Vue Component Debug Info ===');
            console.log('Current tableData:', this.tableData);
            console.log('tableData length:', this.tableData ? this.tableData.length : 'null/undefined');
            console.log('Total:', this.total);
            console.log('Page:', this.page);
            console.log('Table Loading:', this.tableLoading);
            console.log('Show Debug:', this.showDebug);
            console.log('Vue instance $el:', this.$el);
            console.log('================================');
            
            // 测试Vue响应性
            if (this.tableData && this.tableData.length === 0) {
                console.log('测试：添加假数据');
                this.tableData = [
                    {id: 999, red: 255, green: 0, blue: 0, addTime: new Date().toISOString(), lastUpdateTime: new Date().toISOString(), remark: 'test'}
                ];
                this.total = 1;
                setTimeout(() => {
                    console.log('2秒后清除假数据');
                    this.tableData = [];
                    this.total = 0;
                }, 2000);
            }
        }
    }
}); 
```

## Page CSS file sample
File Name: databaseSampleIndex.css
File Path: <ModuleRootPath>/wwwroot/css/Admin/Template_XncfName
Code:
```css
/* 通用样式 */
.d-flex{
    display: flex;
}
.justify-content-between{
    justify-content: space-between;
}
.align-items-center{
    align-items: center;
}

/* 过滤器容器样式 */
.filter-container {
    margin-bottom: 20px;
    padding: 10px 0;
}

.filter-container .el-button {
    margin-right: 10px;
}

/* 颜色预览样式 */
.color-preview {
    width: 100%;
    height: 40px;
    border-radius: 4px;
    border: 1px solid #dcdfe6;
    display: flex;
    align-items: center;
    justify-content: center;
    color: white;
    font-size: 12px;
    font-weight: bold;
    text-shadow: 1px 1px 2px rgba(0,0,0,0.5);
}

.color-preview-large {
    width: 100%;
    height: 80px;
    border-radius: 8px;
    border: 2px solid #dcdfe6;
    display: flex;
    align-items: center;
    justify-content: center;
    color: white;
    font-size: 16px;
    font-weight: bold;
    text-shadow: 2px 2px 4px rgba(0,0,0,0.7);
    margin: 10px 0;
    transition: all 0.3s ease;
}

.color-preview-large:hover {
    transform: scale(1.02);
    box-shadow: 0 4px 12px rgba(0,0,0,0.15);
}

/* 分页容器样式 */
.pagination-container {
    margin-top: 20px;
    text-align: center;
}

/* 表格样式增强 */
.el-table {
    border-radius: 8px;
    overflow: hidden;
    box-shadow: 0 2px 12px 0 rgba(0,0,0,0.1);
}

.el-table th {
    background-color: #fafafa;
    color: #333;
    font-weight: 600;
}

/* 颜色标签样式 */
.el-tag {
    min-width: 50px;
    text-align: center;
    font-weight: bold;
    border: none !important;
    text-shadow: 1px 1px 2px rgba(0,0,0,0.5);
}

/* 对话框样式 */
.el-dialog {
    border-radius: 12px;
    overflow: hidden;
}

.el-dialog__header {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    padding: 20px 20px 0 20px;
}

.el-dialog__title {
    color: white;
    font-weight: 600;
}

.el-dialog__body {
    padding: 30px 20px;
}

/* 滑块样式 */
.el-slider {
    margin: 20px 0;
}

.el-slider__runway {
    height: 6px;
    background-color: #e4e7ed;
    border-radius: 3px;
}

.el-slider__button {
    width: 20px;
    height: 20px;
    border: 2px solid #409eff;
}

/* 按钮样式增强 */
.el-button--mini {
    padding: 5px 10px;
    font-size: 12px;
    border-radius: 4px;
}

.el-button--primary {
    background: linear-gradient(135deg, #409eff 0%, #3a8ee6 100%);
    border: none;
}

.el-button--success {
    background: linear-gradient(135deg, #67c23a 0%, #5daf34 100%);
    border: none;
}

.el-button--warning {
    background: linear-gradient(135deg, #e6a23c 0%, #cf9236 100%);
    border: none;
}

.el-button--danger {
    background: linear-gradient(135deg, #f56c6c 0%, #f25c5c 100%);
    border: none;
}

.el-button--info {
    background: linear-gradient(135deg, #909399 0%, #82848a 100%);
    border: none;
}

/* 表单项样式 */
.el-form-item {
    margin-bottom: 22px;
}

.el-form-item__label {
    font-weight: 600;
    color: #333;
}

/* 加载动画样式 */
.el-loading-mask {
    background-color: rgba(255, 255, 255, 0.9);
}

/* 响应式设计 */
@media (max-width: 768px) {
    .filter-container {
        text-align: center;
    }
    
    .filter-container .el-button {
        margin: 5px;
        width: auto;
    }
    
    .color-preview {
        height: 30px;
        font-size: 10px;
    }
    
    .color-preview-large {
        height: 60px;
        font-size: 14px;
    }
}

/* 动画效果 */
@keyframes fadeIn {
    from {
        opacity: 0;
        transform: translateY(20px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

.el-table tbody tr {
    animation: fadeIn 0.3s ease-out;
}

/* 鼠标悬停效果 */
.el-table tbody tr:hover {
    background-color: #f5f7fa !important;
    transition: background-color 0.3s ease;
}

.el-button:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 8px rgba(0,0,0,0.15);
    transition: all 0.3s ease;
} 
```
";
            return template;

        }

        [McpServerTool, Description("获取后端代码模板示例")]
        public async Task<string> GetBackEndCodeTemplate()
        {
            var template = @"
## Database EntityFramework DbContext class sample
File Name: Template_XncfNameSenparcEntities.cs
File Path: <ModuleRootPath>/Domain/Models/DatabaseModel
Code:
```csharp
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Template_OrgName.Xncf.Template_XncfName.Models.DatabaseModel;

namespace Template_OrgName.Xncf.Template_XncfName.Models
{
    public class Template_XncfNameSenparcEntities : XncfDatabaseDbContext
    {
        public Template_XncfNameSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<Color> Colors { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //如无特殊需需要，OnModelCreating 方法可以不用写，已经在 Register 中要求注册
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
```


## Database Entity class sample
File Name: Color.cs
File Path: <ModuleRootPath>/Domain/Models/DatabaseModel
Code:
```csharp
using Senparc.Ncf.Core.Models;
using Template_OrgName.Xncf.Template_XncfName.Domain.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Template_OrgName.Xncf.Template_XncfName
{
    /// <summary>
    /// Color 实体类
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(Color))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class Color : EntityBase<int>
    {
        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Red { get; private set; }
        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Green { get; private set; }

        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Blue { get; private set; }

        /// <summary>
        /// 附加列，测试多次数据库 Migrate
        /// </summary>
        public string AdditionNote { get; private set; }

        private Color() { }

        public Color(int red, int green, int blue)
        {
            if (red < 0 || green < 0 || blue < 0)
            {
                Random();//随机
            }
            else
            {
                Red = red;
                Green = green;
                Blue = blue;
            }
        }

        public Color(ColorDto colorDto)
        {
            Red = colorDto.Red;
            Green = colorDto.Green;
            Blue = colorDto.Blue;
        }

        public void Random()
        {
            //随机产生颜色代码
            var radom = new Random();
            Func<int> getRadomColorCode = () => radom.Next(0, 255);
            Red = getRadomColorCode();
            Green = getRadomColorCode();
            Blue = getRadomColorCode();
        }
    }
}
```

## Database Entity DTO class sample
File Name: ColorDto.cs
File Path: <ModuleRootPath>/Domain/Models/DatabaseModel/Dto
Code:
```csharp
using Senparc.Ncf.Core.Models;

namespace Template_OrgName.Xncf.Template_XncfName.Domain.Models.DatabaseModel.Dto
{
    public class ColorDto : DtoBase
    {
        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Red { get; set; }
        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Green { get; set; }
        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Blue { get; set; }

        /// <summary>
        /// 附加列，测试多次数据库 Migrate
        /// </summary>
        public string AdditionNote { get; set; }

        public ColorDto() { }
    }
}
```

## Service class sample
File Name: Template_XncfNameService.cs
File Path: <ModuleRootPath>/Domain/Services
Code:
```csharp
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Template_OrgName.Xncf.Template_XncfName.Domain.Services;
using Template_OrgName.Xncf.Template_XncfName.Domain.Models.DatabaseModel.Dto;
using System;
using System.Threading.Tasks;

namespace Template_OrgName.Xncf.Template_XncfName.Domain.Services
{
    public class ColorService : ServiceBase<Color>
    {
        public ColorService(IRepositoryBase<Color> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }

        public async Task<ColorDto> CreateNewColor()
        {
            Color color = new Color(-1, -1, -1);
            await base.SaveObjectAsync(color).ConfigureAwait(false);
            ColorDto colorDto = base.Mapper.Map<ColorDto>(color);
            return colorDto;
        }

        public async Task<ColorDto> GetOrInitColor()
        {
            var color = await base.GetObjectAsync(z => true);
            if (color == null)//如果是纯第一次安装，理论上不会有残留数据
            {
                //创建默认颜色
                ColorDto colorDto = await this.CreateNewColor().ConfigureAwait(false);
                return colorDto;
            }

            return base.Mapper.Map<ColorDto>(color);
        }

        public async Task<ColorDto> Brighten()
        {
            //TODO:异步方法需要添加排序功能
            var obj = await this.GetObjectAsync(z => true, z => z.Id, OrderingType.Descending);
            obj.Brighten();
            await base.SaveObjectAsync(obj).ConfigureAwait(false);
            return base.Mapper.Map<ColorDto>(obj);
        }

        public async Task<ColorDto> Darken()
        {
            //TODO:异步方法需要添加排序功能
            var obj = await this.GetObjectAsync(z => true, z => z.Id, OrderingType.Descending);
            obj.Darken();
            await base.SaveObjectAsync(obj).ConfigureAwait(false);
            return base.Mapper.Map<ColorDto>(obj);
        }

        public async Task<ColorDto> Random()
        {
            //TODO:异步方法需要添加排序功能
            var obj = this.GetObject(z => true, z => z.Id, OrderingType.Descending);
            obj.Random();
            await base.SaveObjectAsync(obj).ConfigureAwait(false);
            return base.Mapper.Map<ColorDto>(obj);
        }

        //TODO: 更多业务方法可以写到这里
    }
}
```
";

            return template;
        }

        [McpServerTool, Description("获取文件内容")]
        public async Task<BuildXncf_GetFileResponse> GetFile([Description("完整模块名，如 Senparc.Xncf.XncfBuilder")] string moduleName, [Description("在模块内的路径+文件名")] string filePath)
        {
            var response = new BuildXncf_GetFileResponse();

            string fullFilePath = null;
            string fileContent = null;
            try
            {
                fullFilePath = this.GetFilePath(moduleName, filePath);

                if (!File.Exists(fullFilePath))
                {
                    throw new Exception("文件不存在：" + filePath);
                }

                fileContent = await File.ReadAllTextAsync(fullFilePath);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                return response;
            }

            response.Success = true;
            response.FileName = Path.GetFileName(fullFilePath);
            response.FilePath = filePath;
            response.FileContent = fileContent;
            return response;
        }

        [McpServerTool, Description("创建或更新文件内容，文件不存在时会自动创建")]
        public async Task<BuildXncf_CreateOrUpdateFileResponse> CreateOrUpdateFile([Description("完整模块名，如 Senparc.Xncf.XncfBuilder")] string moduleName,
           [Description("在模块内的路径+文件名")] string filePath,
           [Description("完整文件内容")] string fullFileContent)
        {
            var response = new BuildXncf_CreateOrUpdateFileResponse();

            string fullFilePath = null;
            string fileContent = null;
            try
            {
                fullFilePath = this.GetFilePath(moduleName, filePath);

                if (!File.Exists(fullFilePath))
                {
                    string directoryPath = Path.GetDirectoryName(fullFilePath);
                    Senparc.CO2NET.Helpers.FileHelper.TryCreateDirectory(directoryPath);
                    response.IsNewFile = true;
                }

                //TODO: 使用 SHA1 验证指纹，把旧文件内容进行缓存或差量备份
                await File.WriteAllTextAsync(fullFilePath, fileContent);
                response.Success = true;
                response.FileName = Path.GetFileName(fullFilePath);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

    }
}
