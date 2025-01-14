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
} 