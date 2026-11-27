/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Index.cshtml related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260731
    Change description: v0.24.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

    Modified by: Senparc - 20261127
    Change description: v0.25.1.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

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

        /// <summary>
        /// Initializes the Weixin-user administration page with account, user, and
        /// tag services used by its page and AJAX handlers.
        /// </summary>
        /// <param name="serviceProvider">The request service provider.</param>
        /// <param name="xncfModuleService">The lazy XNCF module service required by the base page.</param>
        /// <param name="mpAccountService">The public-account persistence service.</param>
        /// <param name="weixinUserService">The Weixin-user persistence service.</param>
        /// <param name="userTagService">The user-tag persistence service.</param>
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

        /// <summary>
        /// Handles the initial Weixin-user page request. The actual account and user list
        /// is loaded by the AJAX endpoint used by the administration view.
        /// </summary>
        /// <param name="mpId">The optional public-account identifier selected by the page.</param>
        /// <param name="pageIndex">The requested one-based page number.</param>
        /// <param name="pageSize">The requested page size.</param>
        /// <returns>The Razor Page result for the Weixin-user administration view.</returns>
        public async Task<IActionResult> OnGetAsync(int mpId = 0, int pageIndex = 1, int pageSize = 20)
        {
            //if (mpId > 0)
            //{
            //    var mpAccount = await _mpAccountService.GetObjectAsync(z => z.Id == mpId);
            //    if (mpAccount == null)
            //    {
            //Legacy comment translated to English.
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
        /// Returns a paged Weixin-user list, optionally filtered by public-account ID,
        /// including tag associations for the AJAX administration view.
        /// </summary>
        /// <param name="mpId">The optional public-account identifier.</param>
        /// <param name="pageIndex">The one-based page number.</param>
        /// <param name="pageSize">The maximum number of users per page.</param>
        /// <returns>An HTTP result containing account data and mapped users.</returns>
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
                        return RenderError(WeixinManagerResource.Format("Account.ConfigurationNotFound", "Official account configuration was not found: {0}", mpId));
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
            /// Adds tags to users returned by the Weixin API.
            /// </summary>
            add,
            /// <summary>
            /// Reconciles all remote tags with persisted tag records.
            /// </summary>
            all
        }

        /// <summary>
        /// Synchronizes remote Weixin users and tags for one configured public account.
        /// The operation pages through remote open identifiers, reconciles local tag records,
        /// and optionally updates user-tag associations according to <paramref name="syncType"/>.
        /// </summary>
        /// <param name="mpId">The configured public-account identifier.</param>
        /// <param name="syncType">The tag synchronization mode.</param>
        /// <returns>A redirect or error result for the synchronization operation.</returns>
        /// <exception cref="InvalidOperationException">Propagated when the remote Weixin API rejects synchronization.</exception>
        public async Task<IActionResult> OnGetSyncUserAsync(int mpId, SyncType syncType)
        {
            var mpAccount = await _mpAccountService.GetObjectAsync(z => z.Id == mpId);
            if (mpAccount == null)
            {
                return RenderError(WeixinManagerResource.Format("Account.ConfigurationNotFound", "Official account configuration was not found: {0}", mpId));
            }

            SenparcTrace.SendCustomLog("开始公众号用户同步", mpAccount.Name);
            //List<WeixinUserDto> weixinUserDtos = new List<WeixinUserDto>();
            string lastOpenId = null;
            List<string> openIds = new List<string>()
            {
                //"olPjZjsbk4WzEbbGDkWWHuwhpg1M"//testID
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

            //synchronizeTag
            var weixinTagDto = new UserTag_CreateOrUpdateDto();
            var allDbUserTags = await _userTagService.GetFullListAsync(z => z.MpAccountId == mpId);
            SenparcTrace.SendCustomLog("当前已经存储UserTags", $"{allDbUserTags.Count} 个\r\n{string.Join(",", allDbUserTags.Select(z => z.Name).ToArray())}");

            var tagInfo = await Senparc.Weixin.MP.AdvancedAPIs.UserTagApi.GetAsync(mpAccount.AppId);
            SenparcTrace.SendCustomLog("微信公众号存储UerTag", $"{tagInfo.tags.Count} 个\r\n{string.Join(",", tagInfo.tags.Select(z => z.name).ToArray())}");

            //Legacy comment translated to English.
            foreach (var tag in tagInfo.tags)
            {
                var dbUserTag = allDbUserTags.FirstOrDefault(z => z.TagId == tag.id);

                UserTag_CreateOrUpdateDto tagDto;
                tagDto = dbUserTag == null
                //Legacy comment translated to English.
                ? new UserTag_CreateOrUpdateDto()
                {
                    Count = tag.count,
                    TagId = tag.id,
                    Name = tag.name,
                    MpAccountId = mpId
                }
                : _userTagService.Mapper.Map<UserTag_CreateOrUpdateDto>(dbUserTag)//fromdatabaseget
                    ;

                tagDto.MpAccountId = mpId;
                tagDto.TagId = tag.id;//Legacy comment translated to English.

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

            //Legacy comment translated to English.
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
            var maxThreadsCount = 300;//Legacy comment translated to English.
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
                        //needupdate，getuserinformation
                        var user = await Senparc.Weixin.MP.AdvancedAPIs.UserApi.InfoAsync(mpAccount.AppId, openId);
                        var weixinUserDto = _weixinUserService.Mapper.Map<WeixinUser_UpdateFromApiDto>(user);
                        weixinUserDto.MpAccountId = mpId;
                        if (weixinUser != null)
                        {
                            //Legacy comment translated to English.
                            var newApiWeixinUserJson = weixinUserDto.ToJson(false, new Newtonsoft.Json.JsonSerializerSettings() { ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore });
                            var oldDbWeixinUserDto = _weixinUserService.Mapper.Map<WeixinUser_UpdateFromApiDto>(weixinUser);
                            oldDbWeixinUserDto.Tagid_List = weixinUser.UserTags_WeixinUsers.Select(z => z.UserTag.TagId).ToArray();
                            var oldDbWeixinUserJson = oldDbWeixinUserDto.ToJson(false, new Newtonsoft.Json.JsonSerializerSettings() { ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore });
                            if (newApiWeixinUserJson != oldDbWeixinUserJson)
                            {
                                SenparcTrace.SendCustomLog("WeixinUserJson 变更", $"旧：{oldDbWeixinUserJson}\r\n新：{newApiWeixinUserJson}");
                                _weixinUserService.Mapper.Map(weixinUserDto, weixinUser);

                                //Legacy comment translated to English.
                                weixinUser.UpdateTime();
                                allToSaveWeixinUsers.Add(weixinUser);//update
                            }

                            //Legacy comment translated to English.
                            foreach (var weixinTagId in user.tagid_list)
                            {
                                var userTag = allDbUserTags.FirstOrDefault(z => z.TagId == weixinTagId);
                                if (userTag == null)
                                {
                                    SenparcTrace.SendCustomLog("匹配到未同步的 TagId", "TagId：" + weixinTagId);//Legacy comment translated to English.
                                }

                                //Legacy comment translated to English.
                                var userTags_WeixinUsers = weixinUser.UserTags_WeixinUsers
                                                    .FirstOrDefault(z => /*z.WeixinUserId == weixinUser.Id &&*/ z.UserTagId == userTag.Id);
                                if (userTags_WeixinUsers == null)
                                {
                                    SenparcTrace.SendCustomLog("发现新Tag关系", $"WeixinUser：{weixinUser.NickName} -> TagName：{userTag.Name}");

                                    var userTag_Weixinuser = new UserTag_WeixinUser(userTag.Id, weixinUser.Id);
                                    userTag_Weixinuser.UpdateTime();
                                    weixinUser.UserTags_WeixinUsers.Add(userTag_Weixinuser);
                                }
                            }

                            //Legacy comment translated to English.
                            var tobeRemoveTagList = weixinUser.UserTags_WeixinUsers.Where(z =>
                                {
                                    var dbUserTag = allDbUserTags.FirstOrDefault(t => t.Id == z.UserTagId);
                                    return !user.tagid_list.Contains(dbUserTag.TagId);
                                });

                            foreach (var userTags_WeixinUsers in tobeRemoveTagList)
                            {
                                SenparcTrace.SendCustomLog("删除Tag关系", $"WeixinUser：{weixinUser.NickName} -> UserTag.Id：{userTags_WeixinUsers.UserTagId}");
                                weixinUser.UserTags_WeixinUsers.Remove(userTags_WeixinUsers);
                            }
                        }
                        else
                        {
                            weixinUser = _weixinUserService.Mapper.Map<Domain.Models.DatabaseModel.WeixinUser>(weixinUserDto);//add
                            weixinUser.UpdateTime();
                            newWeixinUsers.Add(weixinUser);
                            allToSaveWeixinUsers.Add(weixinUser);
                        }

                        //TODO:updategroupinformation


                    }
                });

                tasks[index] = task;

                if (index % maxThreadsCount == 0 || index == openIds.Count - 1)
                {
                    //Legacy comment translated to English.
                    Task.WaitAll(tasks.Values.ToArray());
                    //Legacy comment translated to English.
                    foreach (var item in tasks.Values.Where(z => z.Exception != null))
                    {
                        SenparcTrace.BaseExceptionLog(item.Exception);
                    }
                    //Legacy comment translated to English.
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

            //base.SetMessager(Ncf.Core.Enums.MessageType.success, "updatesuccess！");
            //return RedirectToPage("./Index", new { uid = Uid, mpId = mpId });
            return Ok(new { uid = Uid, mpId });
        }

        /// <summary>
        /// Deletes each existing Weixin-user record identified by the request body.
        /// Missing identifiers are ignored; the last deleted account identifier is
        /// returned for the page's follow-up refresh.
        /// </summary>
        /// <param name="ids">The user identifiers selected for deletion.</param>
        /// <returns>An HTTP 200 result containing the last associated account identifier.</returns>
        /// <exception cref="NullReferenceException">
        /// May be raised when the request body is unavailable and <paramref name="ids"/>
        /// is <see langword="null"/>.
        /// </exception>
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
