using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using System;
using System.Linq;
using System.Threading.Tasks;

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
            string name = request.Content.Length < 5 ? request.Content : request.Content.Substring(0, 5);

            // 更新版本号
            var today = SystemTime.Now;
            var updatedVersion = "";
            if (string.IsNullOrWhiteSpace(request.Version))
            {
                //TODO: 从数据库中获取最新的版本号
                var usedVersionCount = base.GetCount(
                    where: p => p.Version.StartsWith(today.ToString("yy.MM.dd")),
                    ""
                );
                // .GetFullList(
                //     p => p.Version.StartsWith(today.ToString("yy.MM.dd")),
                //     p => p.Version,
                //     Ncf.Core.Enums.OrderingType.Ascending
                // )
                // .Select(p => p.Version)
                // .Count();
                // 使用新版号

                updatedVersion = today.ToString("yyyy.MM.dd." + (usedVersionCount + 1).ToString());
            }
            else // 已有版号，在之前基础上修改
            {
                var lastVersion = base
                    .GetFullList(
                        p => p.Version.StartsWith(today.ToString("yy.MM.dd")),
                        p => p.Version,
                        Ncf.Core.Enums.OrderingType.Ascending
                    );
                updatedVersion = request.Version + "-1";
            }

            PromptItem promptItem = new(name, request.Content, request.ModelId,
                request.TopP, request.Temperature, request.MaxToken, request.FrequencyPenalty, request.PresencePenalty,
                stopSequences: "",
                0, "", "", 0, request.Version ?? "", DateTime.Now);


            //Version = this.GenerateNewVersion(Version).ToString();
            //promptItem.UpdateVersion();

            await base.SaveObjectAsync(promptItem);

            return promptItem;
        }
    }
}