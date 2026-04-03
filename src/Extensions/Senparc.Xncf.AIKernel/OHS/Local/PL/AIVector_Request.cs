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
        /// code name
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        ///name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///Vector database type
        /// </summary>
        public VectorDBType VectorDBType { get; set; }

        /// <summary>
        /// Whether to display
        /// </summary>
        public bool? Show { get; set; }
    }

    public class AIVector_CreateOrEditRequest
    {
        /// <summary>
        /// primary key ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// code name
        /// </summary>
        public string Alias { get; set; }


        /// <summary>
        ///Model name (required)
        /// </summary>
        public string VectorId { get; set; }

        /// <summary>
        ///name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// connection string
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Type of vector database (required), for example: Memory, HardDisk, Redis, Mulivs, Chroma, PostgreSQL, Sqlite, SqlServer, Default
        /// </summary>
        public VectorDBType VectorDBType { get; set; }

        /// <summary>
        ///Note (optional)
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// Whether to share
        /// </summary>
        public bool IsShared { get; set; }


        /// <summary>
        /// Whether to display
        /// </summary>
        public bool Show { get; set; }
    }

    public class AIVector_EditRequest
    {
        /// <summary>
        /// primary key ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// code name
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Whether to display
        /// </summary>
        public bool Show { get; set; }

        /// <summary>
        /// Whether to share
        /// </summary>
        public bool IsShared { get; set; }
    }
}