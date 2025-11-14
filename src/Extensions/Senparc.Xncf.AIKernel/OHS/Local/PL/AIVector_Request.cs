using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Senparc.AI;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.AI.Interfaces;

namespace Senparc.Xncf.AIKernel.OHS.Local.PL
{
    public class AIVector_GetListRequest : PagedRequest
    {
        /// <summary>
        /// 代号
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 向量数据库类型
        /// </summary>
        public VectorDB.VectorDBType VectorDBType { get; set; }

        /// <summary>
        /// 是否显示
        /// </summary>
        public bool? Show { get; set; }
    }

    public class AIVector_CreateOrEditRequest
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
        /// 模型名称（必须）
        /// </summary>
        public string VectorId { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// 向量数据库的类型（必须）, 例如：Memory, HardDisk, Redis, Mulivs, Chroma, PostgreSQL, Sqlite, SqlServer, Default
        /// </summary>
        public VectorDB.VectorDBType VectorDBType { get; set; }

        /// <summary>
        /// Note（可选）
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// 是否共享
        /// </summary>
        public bool IsShared { get; set; }


        /// <summary>
        /// 是否展示
        /// </summary>
        public bool Show { get; set; }
    }

    public class AIVector_EditRequest
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