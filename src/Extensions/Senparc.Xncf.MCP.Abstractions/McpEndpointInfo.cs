/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: McpEndpointInfo.cs
    File description: Related implementation.


    Created by: Senparc - 20260924

    Modified by: Senparc - 20260924
    Change description: v0.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.1.0 0.1.0 feat: add MCP endpoint selection feature in AgentsManager

    Modified by: Senparc - 20261103
    Change description: v0.1.1 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;

namespace Senparc.Xncf.MCP.Abstractions;

/// <summary>
///Legacy comment translated to English.
/// </summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
///Legacy comment translated to English.
///Legacy comment translated to English.
///Legacy comment translated to English.
/// <param name="Description">descriptioninformation。</param>
/// <param name="Enabled">whether enabled。</param>
///Legacy comment translated to English.
/// <param name="LastTestResult">finallytestresult（success/failure）。</param>
///Legacy comment translated to English.
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
