/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenMapicTaskDto.cs
    File description: SenMapicTaskDto related implementation

    Created by: Senparc - 20250114

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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