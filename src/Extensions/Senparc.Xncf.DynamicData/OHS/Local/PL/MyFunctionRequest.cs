/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MyFunctionRequest.cs
    文件功能描述：MyFunctionRequest 相关实现
    
    
    创建标识：Senparc - 20240718
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260717
    修改描述：v0.6.0-preview2 为 DynamicData 模块接入统一资源本地化并优化功能文案

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Ncf.XncfBase;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Senparc.Xncf.DynamicData.OHS.Local.PL
{
    public class MyFunction_CaculateRequest: FunctionAppRequestBase
    {
        [Required]
        [MaxLength(50)]
        [LocalizedDescription(typeof(DynamicDataResource), "Parameter.Sample.Name")]
        public string Name { get; set; }

        [Required]
        [LocalizedDescription(typeof(DynamicDataResource), "Parameter.Sample.Number1")]
        public int Number1 { get; set; }


        [Required]
        [LocalizedDescription(typeof(DynamicDataResource), "Parameter.Sample.Number2")]
        public int Number2 { get; set; }

        [LocalizedDescription(typeof(DynamicDataResource), "Parameter.Sample.Operator")]//下拉列表
           [FunctionParameterUi(ParameterType.DropDownList, nameof(OperatorOptions))]
           public string Operator { get; set; }

           [JsonIgnore]
           public SelectionList OperatorOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new[] {
                 new SelectionItem("+", DynamicDataResource.Get("Parameter.Operator.Add"), DynamicDataResource.Get("Parameter.Operator.Add.Help"), false),
                 new SelectionItem("-", DynamicDataResource.Get("Parameter.Operator.Subtract"), DynamicDataResource.Get("Parameter.Operator.Subtract.Help"), true),
                 new SelectionItem("×", DynamicDataResource.Get("Parameter.Operator.Multiply"), DynamicDataResource.Get("Parameter.Operator.Multiply.Help"), false),
                 new SelectionItem("÷", DynamicDataResource.Get("Parameter.Operator.Divide"), DynamicDataResource.Get("Parameter.Operator.Divide.Help"), false)
            });

        [LocalizedDescription(typeof(DynamicDataResource), "Parameter.Sample.Power")]//多选框
           [FunctionParameterUi(ParameterType.CheckBoxList, nameof(PowerOptions))]
           public string[] Power { get; set; }

           [JsonIgnore]
           public SelectionList PowerOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("2", DynamicDataResource.Get("Parameter.Power.Square"), DynamicDataResource.Get("Parameter.Power.Square.Help"), false),
                 new SelectionItem("3", DynamicDataResource.Get("Parameter.Power.Cube"), DynamicDataResource.Get("Parameter.Power.Cube.Help"), false)
            });
    }
}
