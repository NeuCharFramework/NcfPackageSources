/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：McpEndpointInfo.cs
    文件功能描述：跨模块共享的 MCP Endpoint 只读信息


    创建标识：Senparc - 20260924

    修改标识：Senparc - 20260924
    修改描述：v0.1.0 新增跨模块 MCP Endpoint 信息契约

----------------------------------------------------------------*/

using System;

namespace Senparc.Xncf.MCP.Abstractions;

/// <summary>
/// 跨模块共享的 MCP Endpoint 只读信息（纯数据，与 MCP 模块数据库字段对齐）。
/// </summary>
/// <param name="Id">数据库主键。</param>
/// <param name="Name">端点名称。</param>
/// <param name="Endpoint">端点 URI/路径。</param>
/// <param name="EndpointType">端点类型，例如 http、sse、stdio、websocket。</param>
/// <param name="ProtocolVersion">协议版本。</param>
/// <param name="Description">描述信息。</param>
/// <param name="Enabled">是否启用。</param>
/// <param name="LastTestedTime">最后测试时间。</param>
/// <param name="LastTestResult">最后测试结果（成功/失败）。</param>
/// <param name="LastToolCount">最近一次测试发现的工具数量。</param>
public sealed record McpEndpointInfo(
    int Id,
    string Name,
    string Endpoint,
    string? EndpointType,
    string? ProtocolVersion,
    string? Description,
    bool Enabled,
    DateTime? LastTestedTime,
    bool? LastTestResult,
    int? LastToolCount);
