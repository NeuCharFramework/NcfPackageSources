using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Enums;

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
            if (folder == null) return;
            folder.Name = name;
            folder.Description = description;
            await SaveObjectAsync(folder);
        }

        public async Task DeleteFolderAsync(int id)
        {
            var folder = await GetObjectAsync(z => z.Id == id);
            if (folder == null) return;
            await DeleteObjectAsync(folder);
        }
    }
}
