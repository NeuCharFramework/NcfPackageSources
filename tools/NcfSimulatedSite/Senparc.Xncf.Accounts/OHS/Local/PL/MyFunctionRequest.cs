/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MyFunctionRequest.cs
    文件功能描述：MyFunctionRequest 相关实现
    
    
    创建标识：Senparc - 20211031
    
    修改标识：Senparc - 20260717
    修改描述：v0.3.0 为账户模块接入多语言资源与功能文案本地化

----------------------------------------------------------------*/
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Ncf.Core.AppServices;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Senparc.Xncf.Accounts.OHS.Local.PL
{
    public class MyFunction_CaculateRequest: FunctionAppRequestBase
    {
        [Required]
        [MaxLength(50)]
        [LocalizedDescription(typeof(AccountsResource), "Accounts.Parameter.Name")]
        public string Name { get; set; }

        [Required]
        [LocalizedDescription(typeof(AccountsResource), "Accounts.Parameter.Number1")]
        public int Number1 { get; set; }


        [Required]
        [LocalizedDescription(typeof(AccountsResource), "Accounts.Parameter.Number2")]
        public int Number2 { get; set; }

        [LocalizedDescription(typeof(AccountsResource), "Accounts.Parameter.Operator")]//下拉列表
           [FunctionParameterUi(ParameterType.DropDownList, nameof(OperatorOptions))]
           public string Operator { get; set; }

           [JsonIgnore]
           public SelectionList OperatorOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new[] {
                 new SelectionItem("+", AccountsResource.Get("Accounts.Operator.Add", "加法"), AccountsResource.Get("Accounts.Operator.Add.Help", "数字 1 + 数字 2"), false),
                 new SelectionItem("-", AccountsResource.Get("Accounts.Operator.Subtract", "减法"), AccountsResource.Get("Accounts.Operator.Subtract.Help", "数字 1 - 数字 2"), true),
                 new SelectionItem("×", AccountsResource.Get("Accounts.Operator.Multiply", "乘法"), AccountsResource.Get("Accounts.Operator.Multiply.Help", "数字 1 × 数字 2"), false),
                 new SelectionItem("÷", AccountsResource.Get("Accounts.Operator.Divide", "除法"), AccountsResource.Get("Accounts.Operator.Divide.Help", "数字 1 ÷ 数字 2"), false)
            });

        [LocalizedDescription(typeof(AccountsResource), "Accounts.Parameter.Power")]//多选框
           [FunctionParameterUi(ParameterType.CheckBoxList, nameof(PowerOptions))]
           public string[] Power { get; set; }

           [JsonIgnore]
           public SelectionList PowerOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("2", AccountsResource.Get("Accounts.Power.Square", "平方"), AccountsResource.Get("Accounts.Power.Square.Help", "对上述结果计算平方"), false),
                 new SelectionItem("3", AccountsResource.Get("Accounts.Power.Cube", "三次方"), AccountsResource.Get("Accounts.Power.Cube.Help", "对上述结果计算三次方"), false)
            });
    }
}
