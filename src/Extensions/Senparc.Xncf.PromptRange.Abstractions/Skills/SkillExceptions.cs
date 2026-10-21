/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SkillExceptions.cs
    文件功能描述：Skill 查询与执行异常
    
    
    创建标识：Senparc - 20261016
    
    修改标识：Senparc - 20261021
    修改描述：v0.3.0-preview6 补充 Skill 不存在与访问拒绝异常

----------------------------------------------------------------*/

using System;

namespace Senparc.Xncf.PromptRange.Abstractions.Skills;

public sealed class SkillNotFoundException : Exception
{
    public SkillNotFoundException(string skillId, string version = null)
        : base($"Skill '{skillId}'{(string.IsNullOrWhiteSpace(version) ? string.Empty : $" version '{version}'")} was not found.")
    {
        SkillId = skillId;
        Version = version;
    }

    public string SkillId { get; }
    public string Version { get; }
}

public sealed class SkillAccessDeniedException : Exception
{
    public SkillAccessDeniedException(string skillId, string callerModule)
        : base($"Caller '{callerModule}' is not allowed to use Skill '{skillId}'.")
    {
        SkillId = skillId;
        CallerModule = callerModule;
    }

    public string SkillId { get; }
    public string CallerModule { get; }
}
