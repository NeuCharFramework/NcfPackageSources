/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SenMapicTaskDto.cs
    文件功能描述：SenMapicTaskDto 相关实现
    
    
    创建标识：Senparc - 20250114
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.SenMapic.Models.DatabaseModel.Dto
{
    public class SenMapicTaskDto : DtoBase
    {
        public string Name { get; set; }
        public string StartUrl { get; set; }
        public int MaxThread { get; set; }
        public int MaxBuildMinutes { get; set; }
        public int MaxDeep { get; set; }
        public int MaxPageCount { get; set; }
    }

    public class SenMapicTask_CreateUpdateDto
    {
        public string Name { get; set; }
        public string StartUrl { get; set; }
        public int MaxThread { get; set; }
        public int MaxBuildMinutes { get; set; }
        public int MaxDeep { get; set; }
        public int MaxPageCount { get; set; }
        public bool StartImmediately { get; set; }

    }
} 