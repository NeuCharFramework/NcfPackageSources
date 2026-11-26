/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfFolderService.cs
    File description: NcfFolderService related implementation

    Created by: Senparc - 20251224

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.5.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.6.0-preview1 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.8.5 Localized file management and upload surfaces

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
        /// <summary>
        /// Initializes folder persistence for managed files.
        /// </summary>
        /// <param name="repo">Folder repository.</param>
        /// <param name="serviceProvider">Dependency-injection provider.</param>
        public NcfFolderService(IRepositoryBase<NcfFolder> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// Lists child folders under a parent within one resource scope.
        /// </summary>
        /// <param name="parentId">Optional parent folder identifier.</param>
        /// <param name="resourceScope">The resource boundary.</param>
        /// <returns>Folders ordered by identifier.</returns>
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

        /// <summary>
        /// Resolves or creates a sequence of child folders below a root folder.
        /// </summary>
        /// <param name="folderSegments">Folder names in parent-to-child order.</param>
        /// <param name="rootFolderId">Optional root folder identifier.</param>
        /// <param name="resourceScope">The resource boundary.</param>
        /// <returns>The final folder identifier, or the root identifier when no segments exist.</returns>
        public async Task<int?> GetOrCreateFolderPathAsync(
            IEnumerable<string> folderSegments,
            int? rootFolderId,
            NcfFileResourceScope resourceScope = NcfFileResourceScope.KnowledgeBase)
        {
            EnsureValidScope(resourceScope);
            await ValidateParentAsync(rootFolderId, resourceScope);

            var parentId = rootFolderId;
            foreach (var rawSegment in folderSegments ?? Enumerable.Empty<string>())
            {
                var name = NormalizeName(rawSegment);
                var folder = await GetObjectAsync(z =>
                    z.ParentId == parentId && z.ResourceScope == resourceScope && z.Name == name);
                if (folder == null)
                {
                    folder = await CreateFolderAsync(name, parentId, null, resourceScope);
                }

                parentId = folder.Id;
            }

            return parentId;
        }

        /// <summary>
        /// Validates and creates a folder with a unique sibling name.
        /// </summary>
        /// <param name="name">The folder name.</param>
        /// <param name="parentId">Optional parent folder identifier.</param>
        /// <param name="description">Optional folder description.</param>
        /// <param name="resourceScope">The resource boundary.</param>
        /// <returns>The persisted folder entity.</returns>
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

        /// <summary>
        /// Renames and updates a folder after sibling uniqueness validation.
        /// </summary>
        /// <param name="id">The folder identifier.</param>
        /// <param name="name">The new folder name.</param>
        /// <param name="description">The new description.</param>
        /// <returns>A task that completes after persistence.</returns>
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

        /// <summary>
        /// Deletes an empty folder that has no child folders or files.
        /// </summary>
        /// <param name="id">The folder identifier.</param>
        /// <returns>A task that completes after deletion.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the folder is missing or not empty.</exception>
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

        /// <summary>
        /// Trims a folder name and rejects values that are empty, too long, or
        /// contain path separators or relative-path markers.
        /// </summary>
        /// <param name="name">The folder name supplied by the caller.</param>
        /// <returns>The normalized folder name.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the name is empty, exceeds 200 characters, or could be
        /// interpreted as a path component.
        /// </exception>
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

        /// <summary>
        /// Confirms that a proposed parent folder exists and belongs to the same
        /// <see cref="NcfFileResourceScope"/> as the child folder being created.
        /// A <see langword="null"/> parent identifier represents a root folder.
        /// </summary>
        /// <param name="parentId">The optional parent folder identifier.</param>
        /// <param name="resourceScope">The resource scope required for the parent.</param>
        /// <returns>A task that completes when the parent has been validated.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the parent does not exist or belongs to another resource scope.
        /// </exception>
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

        /// <summary>
        /// Trims a folder description and enforces the persistence length limit.
        /// </summary>
        /// <param name="description">The description supplied by the caller.</param>
        /// <returns>The trimmed description, or <see langword="null"/> when no description was supplied.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the normalized description exceeds 500 characters.
        /// </exception>
        private static string NormalizeDescription(string description)
        {
            var normalized = description?.Trim();
            if (normalized?.Length > 500)
            {
                throw new ArgumentException("文件夹描述不能超过 500 个字符。", nameof(description));
            }
            return normalized;
        }

        /// <summary>
        /// Verifies that a folder name is not already used by a sibling in the
        /// same resource scope.
        /// </summary>
        /// <param name="name">The normalized candidate folder name.</param>
        /// <param name="parentId">The optional parent folder identifier.</param>
        /// <param name="resourceScope">The resource scope in which uniqueness is required.</param>
        /// <returns>A task that completes when the uniqueness check finishes.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when another folder with the same name already exists under the parent.
        /// </exception>
        private async Task EnsureUniqueNameAsync(string name, int? parentId, NcfFileResourceScope resourceScope)
        {
            var duplicate = await GetObjectAsync(z => z.ParentId == parentId && z.Name == name && z.ResourceScope == resourceScope);
            if (duplicate != null)
            {
                throw new InvalidOperationException($"同级目录下已存在名为“{name}”的文件夹。");
            }
        }

        /// <summary>
        /// Validates that a resource scope is one of the values supported by
        /// <see cref="NcfFileResourcePolicy"/>.
        /// </summary>
        /// <param name="resourceScope">The scope to validate.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the scope is not supported.
        /// </exception>
        private static void EnsureValidScope(NcfFileResourceScope resourceScope)
        {
            if (!NcfFileResourcePolicy.IsValidScope(resourceScope))
            {
                throw new ArgumentOutOfRangeException(nameof(resourceScope));
            }
        }
    }
}
