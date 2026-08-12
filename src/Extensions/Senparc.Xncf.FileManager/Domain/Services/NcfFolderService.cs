/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NcfFolderService.cs
    文件功能描述：NcfFolderService 相关实现


    创建标识：Senparc - 20251224

    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260804
    修改描述：v0.5.0-preview5 新增文件文本提取与文件管理服务

    修改标识：Senparc - 20260813
    修改描述：v0.6.0-preview1 完善文件资源边界、安全删除策略与静态资源管理

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

        public async Task<List<NcfFolder>> GetFoldersAsync(
            int? parentId,
            NcfFileResourceScope resourceScope = NcfFileResourceScope.KnowledgeBase)
        {
            EnsureValidScope(resourceScope);
            var list = await GetObjectListAsync(
                1,
                int.MaxValue,
                f => f.ParentId == parentId && f.ResourceScope == resourceScope,
                f => f.Id,
                OrderingType.Ascending,
                null);
            return list;
        }

        /// <summary>
        /// Gets a folder's complete path after verifying that every level belongs
        /// to the requested resource scope. This lets a refreshed page restore
        /// its location without relying on a browser-side, lazily loaded tree.
        /// </summary>
        public async Task<List<NcfFolder>> GetFolderPathAsync(
            int folderId,
            NcfFileResourceScope resourceScope = NcfFileResourceScope.KnowledgeBase)
        {
            EnsureValidScope(resourceScope);
            var path = new List<NcfFolder>();
            var visited = new HashSet<int>();
            int? currentId = folderId;

            while (currentId.HasValue)
            {
                if (!visited.Add(currentId.Value))
                {
                    throw new InvalidOperationException("文件夹层级存在循环，无法读取路径。");
                }

                var folder = await GetObjectAsync(z => z.Id == currentId.Value)
                    ?? throw new InvalidOperationException($"文件夹不存在：{currentId.Value}");
                if (folder.ResourceScope != resourceScope)
                {
                    throw new InvalidOperationException("文件夹的资源用途与当前视图不一致。");
                }

                path.Add(folder);
                currentId = folder.ParentId;
            }

            path.Reverse();
            return path;
        }

        public async Task<NcfFolder> CreateFolderAsync(
            string name,
            int? parentId,
            string description,
            NcfFileResourceScope resourceScope = NcfFileResourceScope.KnowledgeBase)
        {
            name = NormalizeName(name);
            description = NormalizeDescription(description);
            EnsureValidScope(resourceScope);
            await ValidateParentAsync(parentId, resourceScope);
            await EnsureUniqueNameAsync(name, parentId, resourceScope);

            var folder = new NcfFolder
            {
                Name = name,
                ParentId = parentId,
                Description = description,
                ResourceScope = resourceScope,
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
            var duplicate = await GetObjectAsync(z => z.Id != id && z.ParentId == folder.ParentId && z.Name == name && z.ResourceScope == folder.ResourceScope);
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

        private async Task ValidateParentAsync(int? parentId, NcfFileResourceScope resourceScope)
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
            if (parent.ResourceScope != resourceScope)
            {
                throw new InvalidOperationException("父文件夹的资源用途与当前文件夹不一致。");
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

        private async Task EnsureUniqueNameAsync(string name, int? parentId, NcfFileResourceScope resourceScope)
        {
            var duplicate = await GetObjectAsync(z => z.ParentId == parentId && z.Name == name && z.ResourceScope == resourceScope);
            if (duplicate != null)
            {
                throw new InvalidOperationException($"同级目录下已存在名为“{name}”的文件夹。");
            }
        }

        private static void EnsureValidScope(NcfFileResourceScope resourceScope)
        {
            if (!NcfFileResourcePolicy.IsValidScope(resourceScope))
            {
                throw new ArgumentOutOfRangeException(nameof(resourceScope));
            }
        }
    }
}
