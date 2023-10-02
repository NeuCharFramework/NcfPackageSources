using Microsoft.SemanticKernel.SkillDefinition;
using Senparc.AI.Kernel.Handlers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Domain.Services.PromptPlugins
{
    /// <summary>
    /// XNCF 模块生成插件
    /// </summary>
    public sealed class XncfBuilderPlugin
    {
        public XncfBuilderPlugin() { }


        [SKFunction, SKName("BuildEntityClass"), Description("创建实体类")]
        public async Task<string> BuildEntityClass(
            [Description("生成实体类的描述，包括尽可能详细的类名、属性、需要包含的方法等")]string input
            )
        {
            var promptTemplate = @"以下是一段用于生成某个特定功能“EntityFrameworkCore”数据库实体的 C# 代码（以下简称 Entity），此 Entity 的名称为 Color， EntityBase<int> 约定了主键为 int 类型，为了基于 DDD（Domain-Driven Design）的实现，对常规的实体类做了如下的约束：
1. 所有的属性都为只读，即设置 set 属性为 private；
2. 提供一个不带参数的 private 构造函数；
3. 所有的数据默认在一个 public 的构造函数中进行初始化
4. 实体内部包含了必要的方法，形成一个“充血实体”。

# Code Start
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.PromptRange
{
    /// <summary>
    /// Color 实体类
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(Color))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class Color : EntityBase<int>
    {
        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Red { get; private set; }
        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Green { get; private set; }

        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Blue { get; private set; }

        /// <summary>
        /// 附加列，测试多次数据库 Migrate
        /// </summary>
        public string AdditionNote { get; private set; }

        private Color() { }

        public Color(int red, int green, int blue)
        {
            if (red < 0 || green < 0 || blue < 0)
            {
                Random();//随机
            }
            else
            {
                Red = red;
                Green = green;
                Blue = blue;
            }
        }

        public Color(ColorDto colorDto)
        {
            Red = colorDto.Red;
            Green = colorDto.Green;
            Blue = colorDto.Blue;
        }

        public void Random()
        {
            //随机产生颜色代码
            var radom = new Random();
            Func<int> getRadomColorCode = () => radom.Next(0, 255);
            Red = getRadomColorCode();
            Green = getRadomColorCode();
            Blue = getRadomColorCode();
        }

        public void Brighten()
        {
            Red = Math.Min(255, Red + 10);
            Green = Math.Min(255, Green + 10);
            Blue = Math.Min(255, Blue + 10);
        }

        public void Darken()
        {
            Red = Math.Max(0, Red - 10);
            Green = Math.Max(0, Green - 10);
            Blue = Math.Max(0, Blue - 10);
        }
    }
}
# Code End
" +
@$"新生成的类的要求为：{input}";
            return promptTemplate;
        }
    }
}
