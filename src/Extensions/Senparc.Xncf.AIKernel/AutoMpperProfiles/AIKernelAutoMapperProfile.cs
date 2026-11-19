/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIKernelAutoMapperProfile.cs
    File description: AIKernelAutoMapperProfile related implementation

    Created by: Senparc - 20231229

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260915
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

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
            
            CreateMap<AIModel_CreateOrEditRequest, AIModel>();

            CreateMap<AIVector, AIVectorDto>();
            CreateMap<AIVectorDto, AIVector>();

            CreateMap<AIVector_CreateOrEditRequest, AIVector>();

            CreateMap<AITokenUsage, AITokenUsageDto>();
            CreateMap<AITokenUsageDto, AITokenUsage>();
            // CreateMap<AIModel, AIModel_GetDetailResponse>();
            // CreateMap<AIModel, AIModel_GetIdAndNameResponse>();
        }
    }
}