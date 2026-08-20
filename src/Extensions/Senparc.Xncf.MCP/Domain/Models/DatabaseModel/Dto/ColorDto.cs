/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ColorDto.cs
    文件功能描述：ColorDto 相关实现
    
    
    创建标识：Senparc - 20250325
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.MCP.Models.DatabaseModel.Dto
{
    public class ColorDto : DtoBase
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

        private ColorDto() { }
    }
}
