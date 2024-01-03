using AutoMapper;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Xncf.AIKernel;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.AIKernel.OHS.Local.PL;


namespace Senparc.Xncf.AIKernel.AutoMapperProfiles
{
    public class AIKernelAutoMapperProfile : Profile
    {
        public AIKernelAutoMapperProfile()
        {
            CreateMap<AIModel, AIModelDto>();
            CreateMap<AIModelDto, AIModel>();
            
            CreateMap<AIModel_CreateRequest, AIModel>();
            // CreateMap<AIModel, AIModel_GetDetailResponse>();
            CreateMap<AIModel, AIModel_GetIdAndNameResponse>();
        }
    }
}