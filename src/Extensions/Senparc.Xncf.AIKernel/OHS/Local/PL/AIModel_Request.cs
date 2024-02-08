using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Senparc.AI;
using Senparc.Xncf.AIKernel.Domain.Models;

namespace Senparc.Xncf.AIKernel.OHS.Local.PL
{
    public class PagedResponse<T>
    {
        /// <summary>
        /// 总数
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        public IEnumerable<T> Data { get; set; }

        public PagedResponse(int total, IEnumerable<T> data)
        {
            Total = total;
            Data = data;
        }
    }

    public class PagedRequest
    {
        /// <summary>
        /// 页码
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// 每页大小
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        public string Order { get; set; } = "Id desc";
    }

    public class AIModel_GetListRequest : PagedRequest
    {
        /// <summary>
        /// 代号
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// 部署名称
        /// </summary>
        public string DeploymentName { get; set; }

        /// <summary>
        /// 端点
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// AI平台
        /// </summary>
        public AiPlatform AiPlatform { get; set; }

        /// <summary>
        /// 组织ID
        /// </summary>
        public string OrganizationId { get; set; }

        /// <summary>
        /// 是否显示
        /// </summary>
        public bool? Show { get; set; }
    }

    public class AIModel_CreateOrEditRequest
    {
        /// <summary>
        /// 主键 ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 代号
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// 模型名称
        /// </summary>
        public string ModelId { get; set; }

        /// <summary>
        /// 部署名称
        /// </summary>
        public string DeploymentName { get; set; }

        /// <summary>
        /// 端点
        /// </summary>
        public string Endpoint { get; set; } = "";

        /// <summary>
        /// AI平台
        /// </summary>
        public AiPlatform AiPlatform { get; set; }

        /// <summary>
        /// 模型类型
        /// </summary>
        public ConfigModelType ConfigModelType { get; set; }

        /// <summary>
        /// 组织ID
        /// </summary>
        public string OrganizationId { get; set; } = "";

        /// <summary>
        /// API密钥
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// API版本
        /// </summary>
        public string ApiVersion { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// 最大令牌
        /// </summary>
        public int MaxToken { get; set; }

        /// <summary>
        /// 是否显示
        /// </summary>
        public bool Show { get; set; }

        /// <summary>
        /// 是否共享
        /// </summary>
        public bool IsShared { get; set; }
    }

    public class AIModel_EditRequest
    {
        /// <summary>
        /// 主键 ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 代号
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// 是否显示
        /// </summary>
        public bool Show { get; set; }

        /// <summary>
        /// 是否共享
        /// </summary>
        public bool IsShared { get; set; }
    }
}