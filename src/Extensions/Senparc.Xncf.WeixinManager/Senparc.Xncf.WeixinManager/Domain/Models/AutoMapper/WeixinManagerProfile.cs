/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：WeixinManagerProfile.cs
    文件功能描述：WeixinManagerProfile 相关实现
    
    
    创建标识：Senparc - 20250712
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using AutoMapper;
using Senparc.Weixin.MP.AdvancedAPIs.UserTag;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.WeixinManager.Domain.Models.AutoMapper
{
    public class WeixinManagerProfile : Profile
    {
        public WeixinManagerProfile()
        {
            //MpAccount
            CreateMap<MpAccountDto, MpAccount>();
            CreateMap<MpAccount, MpAccountDto>();
            //WeixinUser
            CreateMap<Weixin.MP.AdvancedAPIs.User.UserInfoJson, WeixinUser_UpdateFromApiDto>();
            CreateMap<WeixinUser_UpdateFromApiDto, WeixinUser>();
            CreateMap<WeixinUser, WeixinUser_UpdateFromApiDto>();
            CreateMap<WeixinUserDto, WeixinUser>();
            CreateMap<WeixinUser, WeixinUserDto>();
            //UserTag
            CreateMap<UserTag, UserTag_CreateOrUpdateDto>()
                    .ForSourceMember(z => z.Id, opt =>
                    {
                        //opt.Ignore();
                    });
            CreateMap<TagJson_Tag, UserTag_CreateOrUpdateDto>()
                    .ForMember(z => z.TagId, opt => opt.MapFrom(src => src.id));
            CreateMap<UserTag_CreateOrUpdateDto, UserTag>()
                    .ForMember(z => z.Id, opt =>
                    {
                        opt.Ignore();
                    });
            CreateMap<TagJson_Tag, UserTag>();
            //UserTag_WeixinUser
            CreateMap<UserTag_WeixinUserDto, UserTag_WeixinUser>();
            CreateMap<UserTag_WeixinUser, UserTag_WeixinUserDto>();
        }
    }
}
