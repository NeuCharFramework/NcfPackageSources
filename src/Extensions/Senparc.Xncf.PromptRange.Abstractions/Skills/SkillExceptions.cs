/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: SkillExceptions.cs
    File description: Related implementation.
    
    
    Created by: Senparc - 20261016
    
    Modified by: Senparc - 20261021
    Change description: v0.3.0-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.3.1-preview6 update AdminChat with PromptRange Skill integration

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
