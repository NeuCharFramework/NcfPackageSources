using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.WeixinManager.Domain.Models.VD.Admin.WeixinManager;

namespace Senparc.Xncf.WeixinManager.Areas.Admin.WeixinManager
{
    public class WeixinUser_IndexModel : BaseAdminWeixinManagerModel
    {
        public MpAccountDto MpAccountDto { get; set; }
        public PagedList<WeixinUserDto> WeixinUserDtos { get; set; }

        private readonly ServiceBase<Domain.Models.DatabaseModel.MpAccount> _mpAccountService;
        private readonly ServiceBase<Domain.Models.DatabaseModel.WeixinUser> _weixinUserService;
        private readonly ServiceBase<UserTag> _userTagService;
        private readonly IServiceProvider _serviceProvider;
        private int pageCount = 20;

        public WeixinUser_IndexModel(
            IServiceProvider serviceProvider,
            Lazy<XncfModuleService> xncfModuleService,
            ServiceBase<Domain.Models.DatabaseModel.MpAccount> mpAccountService, ServiceBase<Domain.Models.DatabaseModel.WeixinUser> weixinUserService,
            ServiceBase<UserTag> userTagService
            )
            : base(xncfModuleService)
        {
            _serviceProvider = serviceProvider;
            _mpAccountService = mpAccountService;
            _weixinUserService = weixinUserService;
            _userTagService = userTagService;
        }

        public async Task<IActionResult> OnGetAsync(int mpId = 0, int pageIndex = 1, int pageSize = 20)
        {
            //if (mpId > 0)
            //{
            //    var mpAccount = await _mpAccountService.GetObjectAsync(z => z.Id == mpId);
            //    if (mpAccount == null)
            //    {
            //        return RenderError("公众号配置不存在：" + mpId);
            //    }
            //    MpAccountDto = _mpAccountService.Mapper.Map<MpAccountDto>(mpAccount);
            //}

            //var seh = new Ncf.Utility.SenparcExpressionHelper<Models.WeixinUser>();
            //seh.ValueCompare.AndAlso(MpAccountDto != null, z => z.MpAccountId == MpAccountDto.Id);
            //var where = seh.BuildWhereExpression();
            //var result = await _weixinUserService.GetObjectListAsync(pageIndex, pageSize, where,
            //    z => z.Id, Ncf.Core.Enums.OrderingType.Descending, z => z.Include(p => p.UserTags_WeixinUsers).ThenInclude(p => p.UserTag));

            //ViewData["Test"] = result.FirstOrDefault();
            //WeixinUserDtos = new PagedList<WeixinUserDto>(result.Select(z => _mpAccountService.Mapper.Map<WeixinUserDto>(z)).ToList(), result.PageIndex, result.PageCount, result.TotalCount);
            return Page();
        }

        /// <summary>
        /// handler=Ajax
        /// </summary>
        /// <param name="mpId"></param>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetAjaxAsync(int mpId = 0, int pageIndex = 1, int pageSize = 10)
        {
            try
            {
                MpAccountDto mpAccountDto = null;// new MpAccountDto();
                if (mpId > 0)
                {
                    var mpAccount = await _mpAccountService.GetObjectAsync(z => z.Id == mpId);
                    if (mpAccount == null)
                    {
                        return RenderError("公众号配置不存在：" + mpId);
                    }
                    mpAccountDto = _mpAccountService.Mapper.Map<MpAccountDto>(mpAccount);
                }

                var seh = new Ncf.Utility.SenparcExpressionHelper<Domain.Models.DatabaseModel.WeixinUser>();
                seh.ValueCompare.AndAlso(mpAccountDto != null, z => z.MpAccountId == mpAccountDto.Id);
                var where = seh.BuildWhereExpression();
                var result = await _weixinUserService.GetObjectListAsync(pageIndex, pageSize, where,
                    z => z.Id, Ncf.Core.Enums.OrderingType.Descending, z => z.Include(p => p.UserTags_WeixinUsers).ThenInclude(p => p.UserTag));

                //ViewData["Test"] = result.FirstOrDefault();
                var weixinUserDtos = new PagedList<WeixinUserDto>(result.Select(z => _weixinUserService.Mapper.Map<WeixinUserDto>(z)).ToList(), result.PageIndex, result.PageCount, result.TotalCount);
                return Ok(new
                {
                    mpAccountDto,
                    weixinUserDtos = new
                    {
                        weixinUserDtos.TotalCount,
                        list = weixinUserDtos.Select(z => new
                        {
                            z.AddTime,
                            z.Remark,
                            z.AdminRemark,
                            z.City,
                            z.Country,
                            z.Groupid,
                            z.HeadImgUrl,
                            z.Id,
                            z.Language,
                            z.LastUpdateTime,
                            z.MpAccountId,
                            z.NickName,
                            z.OpenId,
                            z.Province,
                            z.Qr_Scene,
                            z.Qr_Scene_Str,
                            z.Sex,
                            z.Subscribe,
                            z.Subscribe_Scene,
                            Subscribe_Time = new DateTime(1970, 1, 1).AddSeconds(z.Subscribe_Time).ToString(),
                            //_.Tagid_List,
                            z.UnionId,
                            UserTags_WeixinUsers = z.UserTags_WeixinUsers.Select(u => new { u.UserTag, u.UserTagId, u.WeixinUserId }),
                        }).AsEnumerable()
                    }
                });
            }
            catch (Exception ex)
            {
                SenparcTrace.BaseExceptionLog(ex);
                return Ok(false, ex.Message);
            }

        }

        public enum SyncType
        {
            /// <summary>
            /// 只增加未添加的用户
            /// </summary>
            add,
            /// <summary>
            /// 增加未添加的用户，同时更新所有信息（时间会较长）
            /// </summary>
            all
        }

        public async Task<IActionResult> OnGetSyncUserAsync(int mpId, SyncType syncType)
        {
            var mpAccount = await _mpAccountService.GetObjectAsync(z => z.Id == mpId);
            if (mpAccount == null)
            {
                return RenderError("公众号配置不存在：" + mpId);
            }

            SenparcTrace.SendCustomLog("开始公众号用户同步", mpAccount.Name);
            //List<WeixinUserDto> weixinUserDtos = new List<WeixinUserDto>();
            string lastOpenId = null;
            List<string> openIds = new List<string>()
            {
                //"olPjZjsbk4WzEbbGDkWWHuwhpg1M"//测试ID
                //"oxRg0uLsnpHjb8o93uVnwMK_WAVw"
            };
            while (true)
            {
                var result = await Senparc.Weixin.MP.AdvancedAPIs.UserApi.GetAsync(mpAccount.AppId, lastOpenId);
                if (result.data != null)
                {
                    SenparcTrace.SendCustomLog("获取到OpenId", $"{result.data.openid.Count} 个");
                    openIds.AddRange(result.data.openid);
                }

                if (result.next_openid.IsNullOrEmpty())
                {
                    break;
                }
                lastOpenId = result.next_openid;
            }

            //更新Tag
            var weixinTagDto = new UserTag_CreateOrUpdateDto();
            var allDbUserTags = await _userTagService.GetFullListAsync(z => z.MpAccountId == mpId);
            SenparcTrace.SendCustomLog("当前已经存储UserTags", $"{allDbUserTags.Count} 个\r\n{string.Join(",", allDbUserTags.Select(z => z.Name).ToArray())}");

            var tagInfo = await Senparc.Weixin.MP.AdvancedAPIs.UserTagApi.GetAsync(mpAccount.AppId);
            SenparcTrace.SendCustomLog("微信账号存储UerTag", $"{tagInfo.tags.Count} 个\r\n{string.Join(",", tagInfo.tags.Select(z => z.name).ToArray())}");

            //添加或更新 UserTag
            foreach (var tag in tagInfo.tags)
            {
                var dbUserTag = allDbUserTags.FirstOrDefault(z => z.TagId == tag.id);

                UserTag_CreateOrUpdateDto tagDto;
                tagDto = dbUserTag == null
                //? _userTagService.Mapper.Map<UserTag_CreateOrUpdateDto>(tag)//创建新tag
                ? new UserTag_CreateOrUpdateDto()
                {
                    Count = tag.count,
                    TagId = tag.id,
                    Name = tag.name,
                    MpAccountId = mpId
                }
                : _userTagService.Mapper.Map<UserTag_CreateOrUpdateDto>(dbUserTag)//从数据库获取
                    ;

                tagDto.MpAccountId = mpId;
                tagDto.TagId = tag.id;//名称不一致，手动填充

                var changed = false;

                if (dbUserTag == null)
                {
                    SenparcTrace.SendCustomLog("新增UserTag", $"TagId({tagDto.TagId}):{tagDto.Name}");
                    dbUserTag = _userTagService.Mapper.Map<UserTag>(tagDto);
                    //await _userTagService.SaveObjectAsync(dbUserTag);
                    allDbUserTags.Add(dbUserTag);
                    changed = true;
                }
                else
                {
                    SenparcTrace.SendCustomLog("更新UserTag", $"{tagDto.TagId}:{tagDto.Name}");
                    changed = dbUserTag.Update(tagDto);
                }

                if (changed)
                {
                    await _userTagService.SaveObjectAsync(dbUserTag);
                }
            }

            //检测删除的 Tag
            var tobeRemoveTags = allDbUserTags.Where(z => !tagInfo.tags.Exists(t => t.name == z.Name));
            if (tobeRemoveTags.Count() > 0)
            {
                await _userTagService.DeleteAllAsync(tobeRemoveTags);
            }

            var allUsers = await _weixinUserService.GetFullListAsync(z => z.MpAccountId == mpId,
                                     z => z.Include(p => p.UserTags_WeixinUsers), null);

            ConcurrentBag<Domain.Models.DatabaseModel.WeixinUser> allToSaveWeixinUsers = new ConcurrentBag<Domain.Models.DatabaseModel.WeixinUser>();
            ConcurrentBag<Domain.Models.DatabaseModel.WeixinUser> newWeixinUsers = new ConcurrentBag<Domain.Models.DatabaseModel.WeixinUser>();

            ConcurrentDictionary<int, Task> tasks = new ConcurrentDictionary<int, Task>();
            var maxThreadsCount = 300;//最大拉取线程数
            SenparcTrace.SendCustomLog("开始同步用户信息", $"共 {openIds.Count} 个");
            for (int i = 0; i < openIds.Count; i++)
            {
                var index = i;
                var openId = openIds[index];
                Task task = Task.Run(async () =>
                {
                    var weixinUser = allUsers.FirstOrDefault(z => z.OpenId == openId);
                    if (weixinUser == null || syncType == SyncType.all)
                    {
                        //需要新增或尝试更新用户
                        var user = await Senparc.Weixin.MP.AdvancedAPIs.UserApi.InfoAsync(mpAccount.AppId, openId);
                        var weixinUserDto = _weixinUserService.Mapper.Map<WeixinUser_UpdateFromApiDto>(user);
                        weixinUserDto.MpAccountId = mpId;
                        if (weixinUser != null)
                        {
                            //查看已有项更新，两个数据进行对比
                            var newApiWeixinUserJson = weixinUserDto.ToJson(false, new Newtonsoft.Json.JsonSerializerSettings() { ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore });
                            var oldDbWeixinUserDto = _weixinUserService.Mapper.Map<WeixinUser_UpdateFromApiDto>(weixinUser);
                            oldDbWeixinUserDto.Tagid_List = weixinUser.UserTags_WeixinUsers.Select(z => z.UserTag.TagId).ToArray();
                            var oldDbWeixinUserJson = oldDbWeixinUserDto.ToJson(false, new Newtonsoft.Json.JsonSerializerSettings() { ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore });
                            if (newApiWeixinUserJson != oldDbWeixinUserJson)
                            {
                                SenparcTrace.SendCustomLog("WeixinUserJson 更新", $"旧：{oldDbWeixinUserJson}\r\n新：{newApiWeixinUserJson}");
                                _weixinUserService.Mapper.Map(weixinUserDto, weixinUser);

                                //SenparcTrace.SendCustomLog("weixinUser 实体信息", $"{weixinUser.ToJson(false, new Newtonsoft.Json.JsonSerializerSettings() { ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore })}");
                                weixinUser.UpdateTime();
                                allToSaveWeixinUsers.Add(weixinUser);//更新
                            }

                            //添加或删除个人的 Tag
                            foreach (var weixinTagId in user.tagid_list)
                            {
                                var userTag = allDbUserTags.FirstOrDefault(z => z.TagId == weixinTagId);
                                if (userTag == null)
                                {
                                    SenparcTrace.SendCustomLog("匹配到未同步的 TagId", "TagId：" + weixinTagId);//正常情况不会存在
                                }

                                //查找未添加的Tag
                                var userTags_WeixinUsers = weixinUser.UserTags_WeixinUsers
                                                    .FirstOrDefault(z => /*z.WeixinUserId == weixinUser.Id &&*/ z.UserTagId == userTag.Id);
                                if (userTags_WeixinUsers == null)
                                {
                                    SenparcTrace.SendCustomLog("创建新Tag关联", $"WeixinUser：{weixinUser.NickName} -> TagName：{userTag.Name}");

                                    var userTag_Weixinuser = new UserTag_WeixinUser(userTag.Id, weixinUser.Id);
                                    userTag_Weixinuser.UpdateTime();
                                    weixinUser.UserTags_WeixinUsers.Add(userTag_Weixinuser);
                                }
                            }

                            //查找需要删除的Tag
                            var tobeRemoveTagList = weixinUser.UserTags_WeixinUsers.Where(z =>
                                {
                                    var dbUserTag = allDbUserTags.FirstOrDefault(t => t.Id == z.UserTagId);
                                    return !user.tagid_list.Contains(dbUserTag.TagId);
                                });

                            foreach (var userTags_WeixinUsers in tobeRemoveTagList)
                            {
                                SenparcTrace.SendCustomLog("删除Tag关联", $"WeixinUser：{weixinUser.NickName} -> UserTag.Id：{userTags_WeixinUsers.UserTagId}");
                                weixinUser.UserTags_WeixinUsers.Remove(userTags_WeixinUsers);
                            }
                        }
                        else
                        {
                            weixinUser = _weixinUserService.Mapper.Map<Domain.Models.DatabaseModel.WeixinUser>(weixinUserDto);//新增
                            weixinUser.UpdateTime();
                            newWeixinUsers.Add(weixinUser);
                            allToSaveWeixinUsers.Add(weixinUser);
                        }

                        //TODO:更新group信息


                    }
                });

                tasks[index] = task;

                if (index % maxThreadsCount == 0 || index == openIds.Count - 1)
                {
                    //只允许N个线程，否则等待
                    Task.WaitAll(tasks.Values.ToArray());
                    //记录异常
                    foreach (var item in tasks.Values.Where(z => z.Exception != null))
                    {
                        SenparcTrace.BaseExceptionLog(item.Exception);
                    }
                    //清除已完成的线程
                    tasks.Clear();
                }
            }

            SenparcTrace.SendCustomLog("保存用户信息", $"{allToSaveWeixinUsers.Count} 个");
            try
            {
                var saveWeixinusers = allToSaveWeixinUsers.OrderBy(z => z.Subscribe_Time);
                await _weixinUserService.SaveObjectListAsync(saveWeixinusers);
            }
            catch (Exception ex)
            {
                SenparcTrace.BaseExceptionLog(ex);
                SenparcTrace.BaseExceptionLog(ex.InnerException);
            }

            //base.SetMessager(Ncf.Core.Enums.MessageType.success, "更新成功！");
            //return RedirectToPage("./Index", new { uid = Uid, mpId = mpId });
            return Ok(new { uid = Uid, mpId });
        }

        public async Task<IActionResult> OnPostDeleteAsync([FromBody] int[] ids)
        {
            var mpId = 0;
            foreach (var id in ids)
            {
                var weixinUser = await _weixinUserService.GetObjectAsync(z => z.Id == id);
                if (weixinUser != null)
                {
                    mpId = weixinUser.MpAccountId;
                    await _weixinUserService.DeleteObjectAsync(weixinUser);
                }
            }
            //return RedirectToPage("./Index", new { Uid, mpId });
            return Ok(new { Uid, mpId });
        }
    }
}
