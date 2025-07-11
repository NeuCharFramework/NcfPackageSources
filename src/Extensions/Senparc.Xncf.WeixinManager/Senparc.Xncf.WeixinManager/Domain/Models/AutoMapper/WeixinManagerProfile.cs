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
