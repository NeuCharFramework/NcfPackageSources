using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using System;
using System.Linq;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Extensions;
using Senparc.Xncf.PromptRange.OHS.Local.PL.response;

namespace Senparc.Xncf.PromptRange.Domain.Services
{
    public class PromptItemService : ServiceBase<PromptItem>
    {
        public PromptItemService(IRepositoryBase<PromptItem> repo, IServiceProvider serviceProvider) : base(repo,
            serviceProvider)
        {
        }

        public async Task<PromptItem> AddPromptItemAsync(PromptItem_AddRequest request)
        {
            #region validate request dto

            var name = request.Content.SubString(0, 5);

            // 默认值为2000
            request.MaxToken = request.MaxToken > 0 ? request.MaxToken : 2000;
            request.StopSequences = request.StopSequences == "" ? null : request.StopSequences;

            #endregion

            // 更新版本号
            var today = SystemTime.Now;
            var updatedVersion = "";
            if (string.IsNullOrWhiteSpace(request.Version))
            {
                //TODO: 从数据库中获取最新的版本号

                var usedVersionList = (await base.GetFullListAsync(
                    p => p.Version.StartsWith(today.ToString("yyyy.MM.dd")),
                    p => p.Version,
                    Ncf.Core.Enums.OrderingType.Ascending
                )).ToList();
                var cnt = usedVersionList.Count(item => !item.Version.Contains("-"));


                updatedVersion = today.ToString($"yyyy.MM.dd.{cnt + 1}");
            }
            else // 已有版号，在已有上修改
            {
                var usedVersionList = (await base.GetFullListAsync(
                    p => p.Version.StartsWith(request.Version),
                    p => p.Id,
                    Ncf.Core.Enums.OrderingType.Ascending
                )).ToList();
                var cnt = usedVersionList.Count(item => !item.Version.Substring(request.Version.Length).Contains('-'));
                updatedVersion = request.Version + (cnt + 1);
            }

            PromptItem promptItem = new(name, request.Content, request.ModelId,
                request.TopP, request.Temperature, request.MaxToken, request.FrequencyPenalty, request.PresencePenalty,
                stopSequences: "",
                request.NumsOfResults, "", "", 0, updatedVersion, DateTime.Now);


            //Version = this.GenerateNewVersion(Version).ToString();
            //promptItem.UpdateVersion();

            await base.SaveObjectAsync(promptItem);

            return promptItem;
        }
        
        // public async Task<PromptItem> GetByIdAsync(int id)
        // {
        //     return await base.GetObjectAsync(p => p.Id == id);
        // }
    }
}