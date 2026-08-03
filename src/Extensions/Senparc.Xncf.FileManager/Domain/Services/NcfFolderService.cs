/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NcfFolderService.cs
    文件功能描述：NcfFolderService 相关实现
    
    
    创建标识：Senparc - 20251224
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260804
    修改描述：v0.5.0-preview5 新增文件文本提取与文件管理服务

----------------------------------------------------------------*/

using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Senparc.Xncf.FileManager.Domain.Services
{
    public class NcfFolderService : ServiceBase<NcfFolder>
    {
        public NcfFolderService(IRepositoryBase<NcfFolder> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }

        public async Task<List<NcfFolder>> GetFoldersAsync(int? parentId)
        {
            var list = await GetObjectListAsync(1, int.MaxValue, f => f.ParentId == parentId, f => f.Id, OrderingType.Ascending, null);
            return list;
        }

        public async Task<NcfFolder> CreateFolderAsync(string name, int? parentId, string description)
        {
            name = NormalizeName(name);
            description = NormalizeDescription(description);
            await ValidateParentAsync(parentId);
            await EnsureUniqueNameAsync(name, parentId);

            var folder = new NcfFolder
            {
                Name = name,
                ParentId = parentId,
                Description = description,
                CreateTime = DateTime.Now
            };
            await SaveObjectAsync(folder);
            return folder;
        }

        public async Task UpdateFolderAsync(int id, string name, string description)
        {
            var folder = await GetObjectAsync(z => z.Id == id);
            if (folder == null)
            {
                throw new InvalidOperationException($"文件夹不存在：{id}");
            }

            name = NormalizeName(name);
            description = NormalizeDescription(description);
            var duplicate = await GetObjectAsync(z => z.Id != id && z.ParentId == folder.ParentId && z.Name == name);
            if (duplicate != null)
            {
                throw new InvalidOperationException($"同级目录下已存在名为“{name}”的文件夹。");
            }

            folder.Name = name;
            folder.Description = description;
            await SaveObjectAsync(folder);
        }

        public async Task DeleteFolderAsync(int id)
        {
            var folder = await GetObjectAsync(z => z.Id == id);
            if (folder == null)
            {
                throw new InvalidOperationException($"文件夹不存在：{id}");
            }

            var childFolder = await GetObjectAsync(z => z.ParentId == id);
            if (childFolder != null)
            {
                throw new InvalidOperationException("文件夹包含子文件夹，请先移动或删除子文件夹。");
            }

            var fileService = base.ServiceProvider.GetRequiredService<NcfFileService>();
            var childFile = await fileService.GetObjectAsync(z => z.FolderId == id);
            if (childFile != null)
            {
                throw new InvalidOperationException("文件夹包含文件，请先移动或删除文件。");
            }

            await DeleteObjectAsync(folder);
        }

        private static string NormalizeName(string name)
        {
            var normalized = name?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new ArgumentException("文件夹名称不能为空。", nameof(name));
            }
            if (normalized.Length > 200)
            {
                throw new ArgumentException("文件夹名称不能超过 200 个字符。", nameof(name));
            }
            if (normalized is "." or ".." || normalized.IndexOfAny(new[] { '/', '\\' }) >= 0)
            {
                throw new ArgumentException("文件夹名称不能包含路径分隔符或相对路径标记。", nameof(name));
            }
            return normalized;
        }

        private async Task ValidateParentAsync(int? parentId)
        {
            if (!parentId.HasValue)
            {
                return;
            }

            var parent = await GetObjectAsync(z => z.Id == parentId.Value);
            if (parent == null)
            {
                throw new InvalidOperationException($"父文件夹不存在：{parentId.Value}");
            }
        }

        private static string NormalizeDescription(string description)
        {
            var normalized = description?.Trim();
            if (normalized?.Length > 500)
            {
                throw new ArgumentException("文件夹描述不能超过 500 个字符。", nameof(description));
            }
            return normalized;
        }

        private async Task EnsureUniqueNameAsync(string name, int? parentId)
        {
            var duplicate = await GetObjectAsync(z => z.ParentId == parentId && z.Name == name);
            if (duplicate != null)
            {
                throw new InvalidOperationException($"同级目录下已存在名为“{name}”的文件夹。");
            }
        }
    }
}
