using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.AI.Kernel;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.Tests
{
    public class SeedDataGenerator
    {
        /// <summary>
        /// 初始化 PromptRange 数据
        /// </summary>
        public static Action<Dictionary<Type, List<object>>> InitPromptRange = datalist =>
        {
            var gangeNames = new[] {
                "2024.7.12.1",
                "2024.7.12.2"
               };

            List<object> list = new List<object>();

            //打乱顺序
            Random random = new Random();
            while (list.Count < gangeNames.Length)
            {
                var item = gangeNames[random.Next(gangeNames.Length)];
                list.Add(new Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.PromptRange(item, item));
            }

            datalist[typeof(Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.PromptRange)] = list;

            Console.WriteLine($"PromptRange SeedData Init: {list.Count}");
        };

        /// <summary>
        /// 初始化 PromptRange 数据
        /// </summary>
        public static Action<Dictionary<Type, List<object>>> InitPromptItem = datalist =>
           {
               //先初始化 PromptRange
               InitPromptRange(datalist);

               var promptRangeList = datalist[typeof(Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.PromptRange)];
               var defaultPromptRange = promptRangeList.Cast<Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.PromptRange>().First();

               var promptVersions = new[] {
                $"{defaultPromptRange.RangeName}-T1-A1",
                $"{defaultPromptRange.RangeName}-T1-A2",
                $"{defaultPromptRange.RangeName}-T1-A3",
                $"{defaultPromptRange.RangeName}-T2-A1",
                $"{defaultPromptRange.RangeName}-T2-A2",
                $"{defaultPromptRange.RangeName}-T2-A2",
                $"{defaultPromptRange.RangeName}-T2.1-A1",
                $"{defaultPromptRange.RangeName}-T2.1-A2",
                $"{defaultPromptRange.RangeName}-T2.1-A3",
                $"{defaultPromptRange.RangeName}-T2.1-A4",
                $"{defaultPromptRange.RangeName}-T2.1.1-A1",
                $"{defaultPromptRange.RangeName}-T2.2-A1",
                $"{defaultPromptRange.RangeName}-T2.2.1-A1",
                $"{defaultPromptRange.RangeName}-T2.2.1-A2",
                $"{defaultPromptRange.RangeName}-T2.2.1-A3",
               };

               List<object> list = new List<object>();

               //打乱顺序
               Random random = new Random();
               while (list.Count < promptVersions.Length)
               {
                   var item = promptVersions[random.Next(promptVersions.Length)];

                   var versionObject = PromptItem.GetVersionObject(item);
                   list.Add(new PromptItem(
                                new PromptRangeDto()
                                {
                                    RangeName = defaultPromptRange.RangeName
                                },
                                defaultPromptRange.RangeName,
                                versionObject.Tactic));
               }

               datalist[typeof(PromptItem)] = list;
               Console.WriteLine($"PromptItem SeedData Init: {list.Count}");
           };
    }
}
