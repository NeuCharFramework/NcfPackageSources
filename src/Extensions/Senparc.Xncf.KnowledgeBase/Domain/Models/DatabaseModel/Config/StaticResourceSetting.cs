using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.KnowledgeBase.Domain.Models.DatabaseModel.Config
{
    public class StaticResourceSetting
    {
        /// <summary>
        /// Upload root directory, can be separated by soft link
        /// </summary>
        public string RootDir { get; set; }

        /// <summary>
        ///request root directory
        /// </summary>
        public string RequestPath { get; set; }

        /// <summary>
        /// Upload file size, unit: MB
        /// </summary>
        public int MaxSize { get; set; }

        /// <summary>
        ///Domain name visited
        /// </summary>
        public string AccessUrl { get; set; }

        /// <summary>
        /// Allow uploaded files
        /// </summary>
        public string[] AllowFile { get; set; }
    }
}
