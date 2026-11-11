/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ConnectorInstance.cs
    文件功能描述：已保存的连接器账号实体
    
    
    创建标识：Senparc - 20261111
    
    修改标识：Senparc - 20261111
    修改描述：v0.1.0 保存连接器账号与受保护的配置

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.Connectors.Domain.Models.DatabaseModel;

[Table(Register.DATABASE_PREFIX + nameof(ConnectorInstance))]
[Index(nameof(PublicId), IsUnique = true)]
public class ConnectorInstance : EntityBase<int>
{
    [Required, MaxLength(64)]
    public string PublicId { get; private set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; private set; } = string.Empty;

    [Required, MaxLength(80)]
    public string ConnectorKey { get; private set; } = string.Empty;

    public bool Enabled { get; private set; } = true;

    [MaxLength(2000)]
    public string? ConfigJson { get; private set; }

    [MaxLength(1000)]
    public string? EnabledActionsJson { get; private set; }

    [MaxLength(500)]
    public string? Description { get; private set; }

    public bool? LastTestSucceeded { get; private set; }

    [MaxLength(1000)]
    public string? LastTestMessage { get; private set; }

    public DateTime? LastTestedAt { get; private set; }

    private ConnectorInstance()
    {
    }

    public ConnectorInstance(string name, string connectorKey, string? description)
    {
        PublicId = Guid.NewGuid().ToString("N");
        Rename(name, description);
        ConnectorKey = connectorKey.Trim();
        Enabled = true;
    }

    public void Rename(string name, string? description)
    {
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void SetConfiguration(string connectorKey, string? configJson, string? enabledActionsJson, bool enabled)
    {
        ConnectorKey = connectorKey.Trim();
        ConfigJson = configJson;
        EnabledActionsJson = enabledActionsJson;
        Enabled = enabled;
        SetUpdateTime();
    }

    public void RecordTest(bool succeeded, string message)
    {
        LastTestSucceeded = succeeded;
        LastTestMessage = message.Length <= 1000 ? message : message[..1000];
        LastTestedAt = DateTime.UtcNow;
        SetUpdateTime();
    }
}
