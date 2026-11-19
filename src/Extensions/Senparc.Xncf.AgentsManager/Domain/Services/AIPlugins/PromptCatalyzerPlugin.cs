/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptCatalyzerPlugin.cs
    File description: PromptCatalyzerPlugin related implementation

    Created by: Senparc - 20260306

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Senparc.CO2NET;
using Senparc.Xncf.PromptRange.Abstractions.Events;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services.AIPlugins
{
    public class PromptCatalyzerPlugin
    {
        private readonly PromptOptimizationService _optimizationService;

        public PromptCatalyzerPlugin()
        {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            _optimizationService = Senparc.CO2NET.SenparcDI.GetServiceProvider().GetRequiredService<PromptOptimizationService>();
        }

        [KernelFunction, Description("Optimize the given prompt code based on user requirement")]
        public async Task<string> OptimizePrompt(
            [Description("The prompt code to optimize (e.g., 2010.1.2.1-T1-A1)")] string promptCode,
            [Description("The current prompt content")] string promptContent,
            [Description("User's requirement or feedback for optimization")] string requirement,
            [Description("Current optimization context with parameters")] OptimizationContext context
        )
        {
            var result = await _optimizationService.OptimizePromptAsync(promptCode, promptContent, requirement, context);
            
            // Format the result for the LLM
            return $"Optimization Complete. New Prompt Code: {result.NewPromptCode}. Score: {result.Score}. Reason: {result.EvaluationReason}. Content: {result.NewPromptContent}";
        }
    }
}
