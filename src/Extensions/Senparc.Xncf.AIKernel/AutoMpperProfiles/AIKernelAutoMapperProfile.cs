
using AutoMapper;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Xncf.AIKernel;
using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.AIKernel.OHS.Local.PL;



namespace Senparc.Xncf.AIKernel.AutoMpperProfiles
{
	public class AIKernelAutoMapperProfile: Profile
    {
        public AIKernelAutoMapperProfile()
        {
            CreateMap<AIModel, AIModel_CreateOrEditRequest>().ReverseMap();
                        CreateMap<AIModel, AIModel_Response>().ReverseMap();
                        //CreateMap<AIModel, AIModel_ImportExportResponse>().ReverseMap();

            CreateMap<AIModel, AIModel_CreateOrEditRequest>().ReverseMap();
            CreateMap<AIModel, AIModel_Response>().ReverseMap();
            //CreateMap<AIModel, AIModel_ImportExportResponse>().ReverseMap();
        }
    }
}
            