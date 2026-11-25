/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: Register.ChatTool.cs
    File description: AdminChat command registration

    Created by: Senparc - 20261125

----------------------------------------------------------------*/

using Senparc.Areas.Admin.OHS.Local.AppService;
using Senparc.Ncf.Core.Localization;
using Senparc.Ncf.XncfBase.ChatTools;
using System.Collections.Generic;

namespace Senparc.Areas.Admin;

public partial class Register : IChatTool
{
    public IReadOnlyCollection<ChatToolDefinition> GetChatTools()
    {
        return new[]
        {
            new ChatToolDefinition
            {
                Command = "/analyze-system-logs",
                Label = ResourceStringLocalizer.Get(
                    typeof(AdminResource),
                    "AdminChat.Command.AnalyzeSystemLogs",
                    "Analyze system logs"),
                Hint = ResourceStringLocalizer.Get(
                    typeof(AdminResource),
                    "AdminChat.Command.AnalyzeSystemLogsHint",
                    "--date=yyyyMMdd plus your question"),
                ModuleName = MenuName,
                ModuleUid = Uid,
                Parameters = new[]
                {
                    new ChatToolParameterDefinition
                    {
                        Name = "date",
                        Label = ResourceStringLocalizer.Get(
                            typeof(AdminResource),
                            "AdminChat.Command.AnalyzeSystemLogs.Parameter.Date",
                            "Log date"),
                        Hint = ResourceStringLocalizer.Get(
                            typeof(AdminResource),
                            "AdminChat.Command.AnalyzeSystemLogs.Parameter.DateHint",
                            "Optional date in yyyyMMdd; today is used when omitted"),
                        Placeholder = "yyyyMMdd",
                        Prefix = "--date="
                    },
                    new ChatToolParameterDefinition
                    {
                        Name = "question",
                        Label = ResourceStringLocalizer.Get(
                            typeof(AdminResource),
                            "AdminChat.Command.AnalyzeSystemLogs.Parameter.Question",
                            "Analysis question"),
                        Hint = ResourceStringLocalizer.Get(
                            typeof(AdminResource),
                            "AdminChat.Command.AnalyzeSystemLogs.Parameter.QuestionHint",
                            "Describe the symptom or evidence to investigate"),
                        Placeholder = ResourceStringLocalizer.Get(
                            typeof(AdminResource),
                            "AdminChat.Command.AnalyzeSystemLogs.Parameter.QuestionPlaceholder",
                            "Enter an analysis question"),
                        IsPositional = true,
                        Required = true,
                        Order = 2
                    }
                },
                Functions = new[]
                {
                    new ChatToolFunctionDefinition
                    {
                        DeclaringType = typeof(SystemLogAnalysisAppService),
                        MethodName = nameof(SystemLogAnalysisAppService.GetSystemLogOverview)
                    },
                    new ChatToolFunctionDefinition
                    {
                        DeclaringType = typeof(SystemLogAnalysisAppService),
                        MethodName = nameof(SystemLogAnalysisAppService.SearchSystemLogs)
                    },
                    new ChatToolFunctionDefinition
                    {
                        DeclaringType = typeof(SystemLogAnalysisAppService),
                        MethodName = nameof(SystemLogAnalysisAppService.GetSystemLogEvidence)
                    }
                },
                Instructions = """

[System Log Analysis Mode]
The user explicitly requested SenparcTrace analysis. Follow this evidence-driven and token-bounded workflow:
1. Parse date and question from the command. If date is missing, use today's yyyyMMdd date.
2. Your first tool action MUST be GetSystemLogOverview. Do not ask where the logs are or how exceptions are defined; SenparcTrace conventions are already known.
3. Use SearchSystemLogs for focused records; request at most 30 rows per call and prefer exception/type/thread/keyword filters.
4. Call GetSystemLogEvidence only for line numbers that are directly relevant to the conclusion, at most 6 records and 4000 characters per record unless the user explicitly asks for more.
5. Never ask to read the entire log file and never infer from missing data.
6. Every finding must cite date, time, line, thread and the matching tool evidence.
7. Secrets, tokens, passwords and cookies returned by tools are already redacted; do not reconstruct or request original values.
8. Separate observed facts, likely causes and recommended actions. Do not execute write operations.
9. If a query returns too many rows, refine filters before loading more.
10. For a question that can be answered by overview counts, answer directly after that tool result; do not ask clarification.
"""
            }
        };
    }
}
