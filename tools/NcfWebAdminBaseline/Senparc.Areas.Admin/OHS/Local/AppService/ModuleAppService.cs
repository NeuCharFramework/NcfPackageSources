/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ModuleAppService.cs
    File description: Related implementation.

    Created by: Senparc - 20241028

    Modified by: Senparc - 20260724
    Change description: v0.1.0 enhance batch module updates and improve the multilingual management interface

    Modified by: Senparc - 20260729
    Change description: v0.2.0 enhance admin interaction and secure desktop Admin Chat synchronization

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Senparc.Areas.Admin.Areas.Admin.Pages;
using Senparc.Areas.Admin.Domain.Dto;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Areas.Admin.OHS.Local.PL;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Ncf.XncfBase.Threads;
using Senparc.Xncf.XncfModuleManager.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Senparc.Areas.Admin.OHS.Local.PL.Module_GetItemResponse.Response_XncfRegister;
using static Senparc.Areas.Admin.OHS.Local.PL.Module_GetItemResponse.Response_XncfRegister.Response_XncfRegister_RegisteredThreadInfo;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    [BackendJwtAuthorize(BackendJwtAuthorizeAttribute.SuperAdminPolicyName)]
    /// <summary>
    /// Provides super-administrator operations for inspecting, installing,
    /// updating, invoking, and removing XNCF modules.
    /// </summary>
    public class ModuleAppService : LocalAppServiceBase
    {
        private readonly XncfModuleServiceExtension _xncfModuleServiceEx;

        private readonly XncfModuleService _xncfModuleService;

        private readonly IStringLocalizer<AdminResource> _localizer;

        /// <summary>
        /// Initializes the module-management application service.
        /// </summary>
        /// <param name="serviceProvider">The request service provider.</param>
        /// <param name="xncfModuleServiceEx">The extended module service used for discovery and installation.</param>
        /// <param name="xncfModuleService">The persisted module service.</param>
        /// <param name="localizer">The localized message provider.</param>
        public ModuleAppService(IServiceProvider serviceProvider, XncfModuleServiceExtension xncfModuleServiceEx, XncfModuleService xncfModuleService, IStringLocalizer<AdminResource> localizer) : base(serviceProvider)
        {
            _xncfModuleServiceEx = xncfModuleServiceEx;
            _xncfModuleService = xncfModuleService;
            _localizer = localizer;
        }

        /// <summary>
        /// Returns installation, update, new-module, and missing-module counts.
        /// </summary>
        /// <returns>A response containing the current XNCF module statistics.</returns>
        [ApiBind]
        public async Task<AppResponseBase<Module_StatResponse>> StatAsync()
        {
            var response = await this.GetResponseAsync<AppResponseBase<Module_StatResponse>, Module_StatResponse>(async (response, logger) =>
            {
                var installedXncfModules = await _xncfModuleServiceEx.GetFullListAsync(z => true);
                var updateXncfRegisters = _xncfModuleServiceEx.GetUnInstallXncfModule(installedXncfModules);
                var newVersions = updateXncfRegisters.Select(z => _xncfModuleServiceEx.GetVersionDisplayName(installedXncfModules, z));
                var newXncfCount = newVersions.Count(z => !z.Contains("->"));
                var updateVersionXncfCount = newVersions.Count() - newXncfCount;
                var xncfRegisterManager = new XncfRegisterManager(base.ServiceProvider);
                var missingXncfCount = installedXncfModules.Count(z => !XncfRegisterManager.RegisterList.Exists(r => r.Uid == z.Uid));

                var data = new Module_StatResponse()
                {
                    InstalledXncfCount = installedXncfModules.Count,
                    UpdateVersionXncfCount = updateVersionXncfCount,
                    NewXncfCount = newXncfCount,
                    MissingXncfCount = missingXncfCount
                };

                return data;
            });
            return response;
        }

        /// <summary>
        /// Gets all persisted XNCF modules and maps them to administrator DTOs.
        /// </summary>
        /// <returns>A response containing the installed module DTO list.</returns>
        [ApiBind]
        public async Task<AppResponseBase<List<XncfModuleDto>>> GetInstalledListAsync()
        {
            var response = await this.GetResponseAsync<AppResponseBase<List<XncfModuleDto>>, List<XncfModuleDto>>(async (response, logger) =>
            {
                var installedXncfModules = await _xncfModuleServiceEx.GetFullListAsync(z => true);
                var result = installedXncfModules.Select(z => _xncfModuleServiceEx.Mapper.Map<XncfModuleDto>(z)).ToList();
                return result;
            });
            return response;
        }


        /// <summary>
        /// Gets registered modules that are not installed or have a newer version
        /// available than the persisted module record.
        /// </summary>
        /// <returns>A response containing installable or updateable module DTOs.</returns>
        [ApiBind]
        public async Task<AppResponseBase<List<XncfModuleDto>>> GetUnInstalledListAsync()
        {
            var response = await this.GetResponseAsync<AppResponseBase<List<XncfModuleDto>>, List<XncfModuleDto>>(async (response, logger) =>
            {
                var installedXncfModules = await _xncfModuleServiceEx.GetFullListAsync(z => true);
                var updateXncfRegisters = _xncfModuleServiceEx.GetUnInstallXncfModule(installedXncfModules);

                var uninstallOrUpdateXncfModules = updateXncfRegisters
                            .Select(z =>
                            {
                                var installedXncfModule = installedXncfModules.FirstOrDefault(m => m.Uid == z.Uid);
                                var xncfModuleId = installedXncfModule?.Id ?? default;
                                var version = xncfModuleId != default ? $"{installedXncfModule.Version} -> {z.Version}" : z.Version;
                                var state = installedXncfModule != null ? XncfModules_State.更新待审核 : XncfModules_State.关闭;
                                var xncfModuleDto = new XncfModuleDto(xncfModuleId, z.Name, z.Uid, z.MenuName, version, z.Description, null, false, null, z.Icon, state);
                                return xncfModuleDto;
                            }).ToList();

                //var result = updateXncfRegisters.Select(z => z as XncfRegisterBase).ToList();
                return uninstallOrUpdateXncfModules;
            });
            return response;
        }

        /// <summary>
        /// Installs or updates the registered XNCF module identified by its UID.
        /// </summary>
        /// <param name="uid">The stable XNCF module UID.</param>
        /// <returns>A response whose payload is zero after the operation succeeds.</returns>
        /// <exception cref="Exception">
        /// Converted by the standard response pipeline when installation fails.
        /// </exception>
        [ApiBind]
        public async Task<AppResponseBase<int>> InstallXncfModuleAsync(string uid)
        {
            var response = await this.GetResponseAsync<AppResponseBase<int>, int>(async (response, logger) =>
            {
                var result = await _xncfModuleServiceEx.InstallModuleAsync(uid, true);
                return 0;
            });
            return response;
        }



        #region Legacy section

        /// <summary>
        /// Returns detailed metadata for one installed module, including exposed
        /// functions, area links, interfaces, and registered background threads.
        /// </summary>
        /// <param name="uid">The stable XNCF module UID.</param>
        /// <returns>A response containing the module and registration details.</returns>
        /// <exception cref="Exception">
        /// Converted by the response pipeline when the UID is missing, the module
        /// is not persisted, or its registration metadata cannot be loaded.
        /// </exception>
        [ApiBind]
        public async Task<AppResponseBase<Module_GetItemResponse>> GetItemAsync(string uid)
        {
            return await this.GetResponseAsync<AppResponseBase<Module_GetItemResponse>, Module_GetItemResponse>(async (response, logger) =>
            {

                bool mustUpdate = false;
                if (uid.IsNullOrEmpty())
                {
                    throw new Exception(_localizer["Xncf.ModuleIdNotProvided"].Value);
                }


                Ncf.Core.Models.DataBaseModel.XncfModule xncfModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == uid).ConfigureAwait(false);

                if (xncfModule == null)
                {
                    throw new Exception(_localizer["Xncf.ModuleNotAdded"].Value);
                }

                IEnumerable<string> xncfModuleUpdateLog = new List<string>();
                if (!xncfModule.UpdateLog.IsNullOrEmpty())
                {
                    xncfModuleUpdateLog = xncfModule.UpdateLog
                        .Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
                }

                IXncfRegister xncfRegister = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == uid);
                if (xncfRegister == null)
                {
                    throw new Exception(_localizer["Xncf.ModuleMissingOrNotLoaded", XncfRegisterManager.RegisterList.Count].Value);
                }

                IDictionary<(string key, string name, string description), List<FunctionParameterInfo>> functionParameterInfoCollection = new Dictionary<(string key, string name, string description), List<FunctionParameterInfo>>();

                try
                {
                    if (Senparc.Ncf.XncfBase.Register.FunctionRenderCollection.TryGetValue(xncfRegister.GetType(), out var functionGroup))
                    {
                        foreach (var functionBag in functionGroup.Values)
                        {
                            try
                            {
                                var result = await FunctionHelper.GetFunctionParameterInfoAsync(base.ServiceProvider, functionBag, true);

                                var functionKey = functionBag.Key;
                                functionParameterInfoCollection[(functionKey, functionBag.FunctionRenderAttribute.Name, functionBag.FunctionRenderAttribute.Description)] = result;
                            }
                            catch (Exception ex)
                            {
                                SenparcTrace.BaseExceptionLog(ex);
                                throw new Exception(_localizer["Xncf.FunctionLoadError", functionBag.Key].Value);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    SenparcTrace.SendCustomLog("模块读取失败", @$"模块：{xncfModule.Name} / {xncfModule.MenuName} / {xncfModule.Uid}
请尝试更新此模块后刷新页面！\r\n{ex.Message}\r\n{ex.StackTrace}");
                    mustUpdate = true;

                    throw new Exception(_localizer["Xncf.ModuleReadFailed", $"{xncfModule.Name} / {xncfModule.MenuName} / {xncfModule.Uid}"].Value);
                }

                IEnumerable<KeyValuePair<ThreadInfo, Thread>> registeredThreadInfo = xncfRegister.RegisteredThreadInfo;

                var getItemResult = new Module_GetItemResponse()
                {
                    MustUpdate = mustUpdate,
                    XncfModule = _xncfModuleServiceEx.Mapper.Map<XncfModuleDto>(xncfModule),
                    XncfRegister = new Module_GetItemResponse.Response_XncfRegister()
                    {
                        AreaHomeUrl = xncfRegister.GetAreaHomeUrl(),
                        MenuName = xncfRegister.MenuName,
                        Description = xncfRegister.Description,
                        Icon = xncfRegister.Icon,
                        Version = xncfRegister.Version,
                        Uid = xncfRegister.Uid,
                        AreaPageMenuItems = (xncfRegister as Ncf.Core.Areas.IAreaRegister)?.AreaPageMenuItems ?? new List<Ncf.Core.Areas.AreaPageMenuItem>(),
                        Interfaces = xncfRegister.GetType().GetInterfaces().Select(z => z.Name).ToList(),
                        FunctionCount = functionParameterInfoCollection.Count,
                        RegisteredThreadInfo = xncfRegister.RegisteredThreadInfo.Select(z => new Response_XncfRegister_RegisteredThreadInfo()
                        {
                            Key = new RegisteredThreadInfo_Key()
                            {
                                Name = z.Key.Name,
                                StoryHtml = z.Key.StoryHtml
                            },
                            Value = new RegisteredThreadInfo_Value()
                            {
                                IsAlive = z.Value.IsAlive,
                                IsBackground = z.Value.IsAlive ? new bool?(z.Value.IsBackground) : null,
                                ThreadState = z.Value.IsAlive ? new ThreadState?(z.Value.ThreadState) : null,
                                ThreadStateStr = z.Value.IsAlive ? z.Value.ThreadState.ToString() : null
                            }
                        }),
                    },
                    //FunctionParameterInfoCollection = functionParameterInfoCollection.Select(z =>
                    //    new KeyValuePair<(string name, string description), List<FunctionParameterInfo>>(
                    //        (z.Key.name, z.Key.description), z.Value)
                    //    ).OrderBy(z => z.Key.name).ToList(),
                    FunctionParameterInfoCollection = functionParameterInfoCollection
                            .Select(z => new Module_GetItemResponse.Response_FunctionParameterInfoCollection()
                            {
                                Key = new Module_GetItemResponse.Response_FunctionParameterInfoCollection_Key(z.Key.name, z.Key.description),
                                Value = z.Value
                            }).OrderBy(z => z.Key.Name).ToList()
                };

                return getItemResult;
            });
        }


        /// <summary>
        /// Changes the persisted lifecycle state of an XNCF module.
        /// </summary>
        /// <param name="id">The persisted module identifier.</param>
        /// <param name="toState">The new lifecycle state.</param>
        /// <returns>A response containing <c>OK</c> when the state is saved.</returns>
        /// <exception cref="Exception">
        /// Converted by the response pipeline when the module cannot be found
        /// or persistence fails.
        /// </exception>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Put)]
        public async Task<StringAppResponse> ChangeStateAsync(int id, XncfModules_State toState)
        {
            var response = await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var module = await _xncfModuleService.GetObjectAsync(z => z.Id == id).ConfigureAwait(false);

                if (module == null)
                {
                    throw new Exception(_localizer["Xncf.ModuleNotAdded"].Value);
                }

                module.UpdateState(toState);
                await _xncfModuleService.SaveObjectAsync(module).ConfigureAwait(false);
                return "OK";
            });
            return response;
        }

        ///// <summary>
        ///// Get log
        ///// </summary>
        ///// <param name="tempId">Log ID</param>
        ///// <returns></returns>
        //public async Task<StringAppResponse> GetLogAsync(string tempId)
        //{
        //    var response = await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
        //    {
        //        var cache = _serviceProvider.GetObjectCacheStrategyInstance();
        //        var log = await cache.GetAsync<string>(tempId);
        //        if (log == null)
        //        {
        //            return Content("The log file does not exist or has already been downloaded.");
        //        }

        //        await cache.RemoveFromCacheAsync(tempId);

        //        return File(Encoding.UTF8.GetBytes(log), "text/plain", $"xncf-log-{tempId}.txt");
        //    });
        //    return response;
        //}

        /// <summary>
        /// Removes a persisted XNCF module and its menu/permission records.
        /// </summary>
        /// <param name="id">The persisted module identifier.</param>
        /// <returns>A response containing <c>OK</c> after uninstall processing completes.</returns>
        /// <exception cref="Exception">
        /// Converted by the response pipeline when the module is not found or
        /// its uninstall callback fails.
        /// </exception>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Delete)]
        public async Task<StringAppResponse> DeleteAsync(int id)
        {
            var response = await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var module = await _xncfModuleService.GetObjectAsync(z => z.Id == id).ConfigureAwait(false);

                if (module == null)
                {
                    throw new Exception(_localizer["Xncf.ModuleNotAdded"].Value);
                }

                //delete the menu
                Func<Task> uninstall = async () =>
                {
                    //delete the menu
                    Ncf.Service.SysRolePermissionService sysPermissionService = base.ServiceProvider.GetService<Ncf.Service.SysRolePermissionService>();
                    Ncf.Service.SysMenuService sysMenuService = base.ServiceProvider.GetService<Ncf.Service.SysMenuService>();
                    var menu = await sysMenuService.GetObjectAsync(z => z.Id == module.MenuId).ConfigureAwait(false);
                    if (menu != null)
                    {
                        //delete the menu
                        await sysMenuService.DeleteObjectAsync(menu).ConfigureAwait(false);
                        await sysPermissionService.DeleteAllAsync(z => z.PermissionId == menu.Id);
                        await sysMenuService.GetMenuDtoByCacheAsync(true).ConfigureAwait(false);
                    }
                    await _xncfModuleService.DeleteObjectAsync(module).ConfigureAwait(false);
                };

                var register = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == module.Uid);
                if (register == null)
                {
                    await uninstall().ConfigureAwait(false);
                }
                else
                {
                    await register.UninstallAsync(base.ServiceProvider, uninstall).ConfigureAwait(false);
                }

                return "OK";
            });

            return response;
        }


        /// <summary>
        /// Invokes a registered module function using the supplied serialized
        /// request parameters and returns its application response data.
        /// </summary>
        /// <param name="executeFuncParamDto2">
        /// The module UID, function display name, and serialized function parameters.
        /// </param>
        /// <returns>
        /// A response containing the function output, log correlation identifier,
        /// and any application-level error message.
        /// </returns>
        /// <exception cref="Exception">
        /// Converted by the response pipeline when function resolution, parameter
        /// normalization, invocation, or result extraction fails.
        /// </exception>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        public async Task<AppResponseBase<Module_RunFunctionResponse>> RunFunctionAsync([FromBody] ExecuteFuncParamDto2 executeFuncParamDto2)
        {
            return await this.GetResponseAsync<AppResponseBase<Module_RunFunctionResponse>, Module_RunFunctionResponse>(async (response, logger) =>
            {
                var xncfRegister = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == executeFuncParamDto2.XncfUid);

                if (xncfRegister == null)
                {
                    response.Success = false;
                    response.ErrorMessage = _localizer["Xncf.ModuleNotRegistered"].Value;
                    return null;
                }

                var xncfModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == xncfRegister.Uid).ConfigureAwait(false);
                if (xncfModule == null)
                {
                    response.Success = false;
                    response.ErrorMessage = _localizer["Xncf.ModuleNotInstalled"].Value;
                    return null;
                }

                if (xncfModule.State != XncfModules_State.开放)
                {
                    response.Success = false;
                    response.ErrorMessage = string.Format(_localizer["Xncf.InvalidModuleState"].Value, xncfModule.State);
                    return null;
                }

                FunctionRenderBag? rightFunctionBag = null;
                if (Senparc.Ncf.XncfBase.Register.FunctionRenderCollection.TryGetValue(xncfRegister.GetType(), out var functionGroup))
                {
                    foreach (var funtionBag in functionGroup.Values)
                    {
                        var funClass = base.ServiceProvider.GetService(funtionBag.MethodInfo.DeclaringType) as IAppService;
                        //var funMethod = funClass.GetType().GetMethod()

                        if (funtionBag.FunctionRenderAttribute.Name == executeFuncParamDto2.XncfFunctionName)
                        {
                            rightFunctionBag = funtionBag;
                            break;
                        }
                    }
                }

                if (rightFunctionBag == null)
                {
                    response.Success = false;
                    response.ErrorMessage = _localizer["Xncf.FunctionNotMatched"].Value;
                    return null;
                }

                var functionParameterType = rightFunctionBag.Value.MethodInfo.GetParameters().FirstOrDefault()?.ParameterType;
                if (functionParameterType == null)
                {
                    functionParameterType = typeof(FunctionAppRequestBase);
                }

                var paramCount = rightFunctionBag.Value.MethodInfo.GetParameters().Length;
                object[] paras = null;
                switch (paramCount)
                {
                    case 1:
                        var normalizedJson = FunctionRequestParameterNormalizer.NormalizeJson(executeFuncParamDto2.XncfFunctionParams, functionParameterType);
                        var requestPara = SerializerHelper.GetObject(normalizedJson, functionParameterType) as IAppRequest;
                        paras = new[] { requestPara };
                        break;
                    case 0:
                        //nothandling
                        break;
                    default:
                        {
                            response.Success = false;
                            response.ErrorMessage = _localizer["Xncf.FunctionSingleParameterOnly"].Value;
                            return null;
                        }
                }

                var functionClass = base.ServiceProvider.GetService(rightFunctionBag.Value.MethodInfo.DeclaringType);
                var taskFunc = rightFunctionBag.Value.MethodInfo.Invoke(functionClass, paras) as Task; // Task<StringAppResponse>  as IAppResponse;
                await taskFunc.ConfigureAwait(false);


                var taskResult = (object)((dynamic)taskFunc).Result;
                var result = taskResult as IAppResponse;

                var returnData = result.Data is string stringData ? stringData.HtmlEncode() : result.Data?.ToJson().HtmlEncode();

                var data = new Module_RunFunctionResponse
                {
                    Msg = returnData,
                    Log = returnData,
                    Exception = result.ErrorMessage,
                    TempId = result.RequestTempId
                };
                return data;
            });
        }


        #endregion
    }
}
