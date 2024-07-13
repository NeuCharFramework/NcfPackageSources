using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.AI.Kernel;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.UnitTestExtension.Entities;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.Tests
{
    public class SeedDataGenerator
    {
        /// <summary>
        /// 初始化 PromptRange 数据
        /// </summary>
        public static Action<DataList> InitPromptRange = datalist =>
        {
            var rangeNames = new[] {
                "2024.7.12.1",
                "2024.7.12.2"
               };

            List<Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.PromptRange> orderedList = new();
            for (int i = 1; i <= rangeNames.Length; i++)
            {
                var item = rangeNames[i - 1];
                var promptRange = new Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.PromptRange(item, $"Range {item}");
                promptRange.Id = i;
                orderedList.Add(promptRange);
            }

            List<object> list = new List<object>();

            //打乱顺序
            Random random = new Random();
            while (list.Count < rangeNames.Length)
            {
                var item = orderedList[random.Next(orderedList.Count)];
                orderedList.Remove(item);
                list.Add(item);
            }

            datalist[typeof(Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.PromptRange)] = list;

            Console.WriteLine($"PromptRange SeedData Init: {list.Count}");
        };

        /// <summary>
        /// 初始化 PromptRange 数据
        /// </summary>
        public static Action<DataList> InitPromptItem = datalist =>
           {
               //先初始化 PromptRange
               InitPromptRange(datalist);

               var promptRangeList = datalist[typeof(Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.PromptRange)]
                                        .Cast<Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.PromptRange>();
               var firstPromptRange = promptRangeList.First();
               var secondPromptRange = promptRangeList.Skip(1).First();

               //Version , parentTactic
               var promptVersions = new Dictionary<string, string> {
               { $"{firstPromptRange.RangeName}-T1-A1",    ""},
               { $"{firstPromptRange.RangeName}-T1-A2",    ""},
               { $"{firstPromptRange.RangeName}-T1-A3",    ""},
               { $"{firstPromptRange.RangeName}-T2-A1",    ""},
               { $"{firstPromptRange.RangeName}-T2-A2",    ""},
               { $"{firstPromptRange.RangeName}-T2-A3",    ""},
               { $"{firstPromptRange.RangeName}-T2.1-A1",  "2"},
               { $"{firstPromptRange.RangeName}-T2.1-A2",  "2"},
               { $"{firstPromptRange.RangeName}-T2.1-A3",  "2"},
               { $"{firstPromptRange.RangeName}-T2.1-A4",  "2"},
               { $"{firstPromptRange.RangeName}-T2.1.1-A1","2.1"},
               { $"{firstPromptRange.RangeName}-T2.2-A1",  "2"},
               { $"{firstPromptRange.RangeName}-T2.2.1-A1","2.2"},
               { $"{firstPromptRange.RangeName}-T2.2.1-A2","2.2"},
               { $"{firstPromptRange.RangeName}-T2.2.1-A3","2.2"},
               { $"{firstPromptRange.RangeName}-T2.2.1-A4","2.2"},
               { $"{firstPromptRange.RangeName}-T2.2.2-A1","2.2"},
               { $"{firstPromptRange.RangeName}-T3-A1",    ""},
               { $"{firstPromptRange.RangeName}-T3.1-A1",  "3"},
               { $"{firstPromptRange.RangeName}-T3.1.1-A1","3.1"},
               { $"{firstPromptRange.RangeName}-T3-A2",    ""},
               { $"{firstPromptRange.RangeName}-T3.2-A1",  "3"},
               { $"{firstPromptRange.RangeName}-T3.1.1-A2","3.1"},

               { $"{secondPromptRange.RangeName}-T1-A1",   ""},
               { $"{secondPromptRange.RangeName}-T1-A2",   ""},
               { $"{secondPromptRange.RangeName}-T1.1-A1", "1"},
               };

               List<PromptItem> promptItems = new List<PromptItem>();
               int i = 0;

               foreach (var item in promptVersions)
               {
                   var versionObject = PromptItem.GetVersionObject(item.Key);

                   var range = promptRangeList.First(z => z.RangeName == versionObject.RangeName);
                   var promptItem = new PromptItem(
                                new PromptItemDto()
                                {
                                    Id = ++i,
                                    RangeName = versionObject.RangeName,
                                    RangeId = range.Id,
                                    Tactic = versionObject.Tactic,
                                    Aiming = versionObject.Aim,
                                    ParentTac = PromptItem.GetParentTasticFromTastic(versionObject.Tactic)
                                });

                   if (promptItem.ParentTac != item.Value)
                   {
                       Assert.Fail("ParentTac 定义错误：" + item.ToJson());
                   }

                   promptItems.Add(promptItem);
               }

               List<object> list = new List<object>();

               //打乱顺序
               Random random = new Random();
               while (list.Count < promptVersions.Keys.Count)
               {
                   var index = random.Next(promptItems.Count);
                   var item = promptItems[index];
                   promptItems.RemoveAt(index);

                   var versionObject = PromptItem.GetVersionObject(item.FullVersion);

                   list.Add(item);
               }

               datalist[typeof(PromptItem)] = list;
               Console.WriteLine($"PromptItem SeedData Init: {list.Count}");
           };
    }
}
