/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptCatalyzerPlugin.cs
    文件功能描述：PromptCatalyzerPlugin 相关实现
    
    
    创建标识：Senparc - 20260306
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
            // 尝试从 DI 容器获取服务，确保在调用时 ServiceProvider 已就绪
            // 注意：Constructor 注入在 Kernel.Plugins.AddFromType<T>() 时可能需要 Kernel 自身配置了 DI
            // 这里为了保险起见，使用 ServiceLocator 作为 backup
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
