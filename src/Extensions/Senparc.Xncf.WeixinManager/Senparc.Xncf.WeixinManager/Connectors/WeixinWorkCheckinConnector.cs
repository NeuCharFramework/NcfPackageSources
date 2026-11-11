/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：WeixinWorkCheckinConnector.cs
    文件功能描述：提供企业微信打卡连接器
    
    
    创建标识：Senparc - 20261111
    
    修改标识：Senparc - 20261111
    修改描述：v0.24.18 提供企业微信打卡连接器

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Weixin.Work.AdvancedAPIs.OaDataOpen;
using Senparc.Weixin.Work.CommonAPIs;
using Senparc.Xncf.Connectors.Abstractions;

namespace Senparc.Xncf.WeixinManager.Connectors;

/// <summary>
/// 企业微信打卡连接器。一个类对应一种外部能力，实例上再保存 CorpId 和应用 Secret。
/// </summary>
public sealed class WeixinWorkCheckinConnector : IConnectorProvider
{
    public const string Key = "weixin.work.checkin";
    public const string GetRecordsAction = "get-checkin-records";

    public ConnectorDescriptor Describe() => new()
    {
        Key = Key,
        Name = "企业微信打卡",
        Description = "使用企业微信应用的 CorpId 和 Secret 获取打卡记录。配置后可在对话、Workflow 和 Agent 模板中选择。",
        Icon = "fa fa-weixin",
        Fields =
        [
            new ConnectorFieldDescriptor
            {
                Key = "corpId",
                Label = "企业 ID",
                Description = "企业微信管理后台的 CorpId。",
                Required = true,
                Placeholder = "ww1234567890abcdef"
            },
            new ConnectorFieldDescriptor
            {
                Key = "corpSecret",
                Label = "应用 Secret",
                Description = "需要具备打卡数据权限的自建应用 Secret。编辑时留空会继续使用已保存的密钥。",
                Kind = ConnectorFieldKind.Secret,
                Required = true,
                Placeholder = "应用 Secret"
            },
            new ConnectorFieldDescriptor
            {
                Key = "defaultCheckinType",
                Label = "默认打卡类型",
                Description = "调用时如果没有另外指定，就使用这个类型。",
                Kind = ConnectorFieldKind.Select,
                Options =
                [
                    new ConnectorOption { Value = "3", Label = "全部打卡" },
                    new ConnectorOption { Value = "1", Label = "上下班打卡" },
                    new ConnectorOption { Value = "2", Label = "外出打卡" }
                ]
            }
        ],
        Actions =
        [
            new ConnectorActionDescriptor
            {
                Key = GetRecordsAction,
                Name = "获取打卡记录",
                Description = "按成员账号和时间范围获取企业微信打卡记录，单次最长 30 天。",
                Parameters =
                [
                    new ConnectorParameterDescriptor
                    {
                        Key = "userIds",
                        Label = "成员账号",
                        Description = "企业微信 userid，多个用逗号或空格分隔。",
                        Required = true
                    },
                    new ConnectorParameterDescriptor
                    {
                        Key = "startTime",
                        Label = "开始时间",
                        Description = "例如 2026-11-01 或 2026-11-01 09:00。留空则从今天 0 点开始。"
                    },
                    new ConnectorParameterDescriptor
                    {
                        Key = "endTime",
                        Label = "结束时间",
                        Description = "留空则到当前时间。"
                    },
                    new ConnectorParameterDescriptor
                    {
                        Key = "checkinType",
                        Label = "打卡类型",
                        Description = "1 上下班，2 外出，3 全部。"
                    }
                ]
            }
        ]
    };

    public async Task<ConnectorTestResult> TestAsync(
        IReadOnlyDictionary<string, string> configuration,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!configuration.TryGetValue("corpId", out var corpId) || string.IsNullOrWhiteSpace(corpId)
            || !configuration.TryGetValue("corpSecret", out var corpSecret) || string.IsNullOrWhiteSpace(corpSecret))
        {
            return new ConnectorTestResult(false, "请先填写企业 ID 和应用 Secret。");
        }

        try
        {
            var token = await CommonApi.GetTokenAsync(corpId.Trim(), corpSecret.Trim()).ConfigureAwait(false);
            var code = Convert.ToInt32(token.errcode, CultureInfo.InvariantCulture);
            Console.WriteLine($"[Connector] weixin.work.checkin test corpId={corpId.Trim()} errcode={code}");
            return code == 0
                ? new ConnectorTestResult(true, "企业微信凭证有效，可以获取打卡记录。")
                : new ConnectorTestResult(false, $"企业微信拒绝了凭证：{token.errmsg}");
        }
        catch (Exception exception)
        {
            Console.WriteLine($"[Connector] weixin.work.checkin test failed: {exception.Message}");
            return new ConnectorTestResult(false, "无法连接企业微信，请检查 CorpId、Secret 和服务器网络。");
        }
    }

    public async Task<ConnectorInvocationResult> InvokeAsync(
        ConnectorInvocationContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(context.ActionKey, GetRecordsAction, StringComparison.OrdinalIgnoreCase))
        {
            return ConnectorInvocationResult.Fail("企业微信打卡连接器目前只提供获取打卡记录。");
        }

        var defaultType = ReadInt(context.Configuration, "defaultCheckinType", 3);
        if (!WeixinWorkCheckinArguments.TryCreate(context.Arguments, defaultType, out var query, out var error))
        {
            return ConnectorInvocationResult.Fail(error ?? "打卡查询参数无效。");
        }

        if (!context.Configuration.TryGetValue("corpId", out var corpId)
            || !context.Configuration.TryGetValue("corpSecret", out var corpSecret))
        {
            return ConnectorInvocationResult.Fail("连接器缺少企业 ID 或应用 Secret。");
        }

        try
        {
            var token = await CommonApi.GetTokenAsync(corpId.Trim(), corpSecret.Trim()).ConfigureAwait(false);
            if (Convert.ToInt32(token.errcode, CultureInfo.InvariantCulture) != 0 || string.IsNullOrWhiteSpace(token.access_token))
            {
                return ConnectorInvocationResult.Fail($"获取企业微信访问凭证失败：{token.errmsg}");
            }

            var result = await OaDataOpenApi.GetCheckinDataAsync(
                token.access_token,
                (OaDataOpenApi.OpenCheckinDataType)query.CheckinType,
                query.Start,
                query.End,
                query.UserIds,
                10000).ConfigureAwait(false);
            var code = Convert.ToInt32(result.errcode, CultureInfo.InvariantCulture);
            Console.WriteLine($"[Connector] weixin.work.checkin records corpId={corpId.Trim()} users={query.UserIds.Length} errcode={code}");
            if (code != 0)
            {
                return ConnectorInvocationResult.Fail($"企业微信返回错误：{result.errmsg}");
            }

            var checkinData = result.checkindata ?? [];
            var records = checkinData
                .Take(50)
                .Select(item => new
                {
                    userId = item.userid,
                    groupName = item.groupname,
                    checkinType = item.checkin_type,
                    exceptionType = item.exception_type,
                    checkinTime = FormatUnixTime(item.checkin_time),
                    location = item.location_title
                })
                .ToArray();
            var output = JsonSerializer.Serialize(new
            {
                connector = context.InstanceName,
                total = checkinData.Length,
                returned = records.Length,
                start = query.Start.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                end = query.End.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                records
            });
            return ConnectorInvocationResult.Ok(output);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"[Connector] weixin.work.checkin invoke failed: {exception.Message}");
            return ConnectorInvocationResult.Fail($"获取打卡记录失败，请确认应用有打卡权限，且成员账号正确。错误信息：{exception.Message}");
        }
    }

    private static int ReadInt(IReadOnlyDictionary<string, string> values, string key, int fallback)
        => values.TryGetValue(key, out var raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;

    private static string FormatUnixTime(long unixTime)
    {
        if (unixTime <= 0)
        {
            return string.Empty;
        }

        return DateTimeOffset.FromUnixTimeSeconds(unixTime).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }
}

public static class WeixinWorkCheckinArguments
{
    public static bool TryCreate(
        IReadOnlyDictionary<string, string> arguments,
        int defaultCheckinType,
        out WeixinWorkCheckinQuery query,
        out string? error)
    {
        query = new WeixinWorkCheckinQuery(DateTime.Today, DateTime.Now, [], defaultCheckinType);
        var userIds = SplitUserIds(Read(arguments, "userIds"));
        if (userIds.Length == 0)
        {
            error = "请提供至少一名企业微信成员的 userid。";
            return false;
        }

        if (userIds.Length > 100)
        {
            error = "单次最多查询 100 名成员。";
            return false;
        }

        var now = DateTime.Now;
        if (!TryReadTime(Read(arguments, "startTime"), DateTime.Today, out var start)
            || !TryReadTime(Read(arguments, "endTime"), now, out var end))
        {
            error = "时间格式无法识别，请使用 2026-11-01 或 2026-11-01 09:00。";
            return false;
        }

        if (end < start)
        {
            error = "结束时间需要晚于开始时间。";
            return false;
        }

        if (end - start > TimeSpan.FromDays(30))
        {
            error = "企业微信单次最多获取 30 天打卡记录。";
            return false;
        }

        var checkinType = defaultCheckinType;
        var rawType = Read(arguments, "checkinType");
        if (!string.IsNullOrWhiteSpace(rawType))
        {
            if (!int.TryParse(rawType, NumberStyles.Integer, CultureInfo.InvariantCulture, out checkinType)
                || checkinType is < 1 or > 3)
            {
                error = "打卡类型只能是 1（上下班）、2（外出）或 3（全部）。";
                return false;
            }
        }
        else if (checkinType is < 1 or > 3)
        {
            checkinType = 3;
        }

        query = new WeixinWorkCheckinQuery(start, end, userIds, checkinType);
        error = null;
        return true;
    }

    public static string[] SplitUserIds(string? raw)
        => string.IsNullOrWhiteSpace(raw)
            ? []
            : raw.Split([',', '，', ';', '；', '\n', '\t', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

    private static string Read(IReadOnlyDictionary<string, string> arguments, string key)
        => arguments.TryGetValue(key, out var value) ? value : string.Empty;

    private static bool TryReadTime(string raw, DateTime fallback, out DateTime value)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = fallback;
            return true;
        }

        return DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out value)
               || DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out value);
    }
}

public sealed record WeixinWorkCheckinQuery(DateTime Start, DateTime End, string[] UserIds, int CheckinType);
