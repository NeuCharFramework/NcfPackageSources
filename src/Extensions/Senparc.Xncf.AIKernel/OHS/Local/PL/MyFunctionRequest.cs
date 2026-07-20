/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MyFunctionRequest.cs
    文件功能描述：MyFunctionRequest 相关实现
    
    
    创建标识：Senparc - 20231220
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260717
    修改描述：v0.14.0-preview5 为 AIKernel 模块接入统一资源本地化并优化功能文案

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Senparc.Xncf.AIKernel.OHS.Local.PL
{
    public class MyFunction_CaculateRequest: FunctionAppRequestBase
    {
        [Required]
        [MaxLength(50)]
        [LocalizedDescription(typeof(AIKernelResource), "Parameter.Sample.Name")]
        public string Name { get; set; }

        [Required]
        [LocalizedDescription(typeof(AIKernelResource), "Parameter.Sample.Number1")]
        public int Number1 { get; set; }


        [Required]
        [LocalizedDescription(typeof(AIKernelResource), "Parameter.Sample.Number2")]
        public int Number2 { get; set; }

        [LocalizedDescription(typeof(AIKernelResource), "Parameter.Sample.Operator")]//下拉列表
           [FunctionParameterUi(ParameterType.DropDownList, nameof(OperatorOptions))]
           public string Operator { get; set; }

           [JsonIgnore]
           public SelectionList OperatorOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new[] {
                 new SelectionItem("+", AIKernelResource.Get("Parameter.Operator.Add"), AIKernelResource.Get("Parameter.Operator.Add.Help"), false),
                 new SelectionItem("-", AIKernelResource.Get("Parameter.Operator.Subtract"), AIKernelResource.Get("Parameter.Operator.Subtract.Help"), true),
                 new SelectionItem("×", AIKernelResource.Get("Parameter.Operator.Multiply"), AIKernelResource.Get("Parameter.Operator.Multiply.Help"), false),
                 new SelectionItem("÷", AIKernelResource.Get("Parameter.Operator.Divide"), AIKernelResource.Get("Parameter.Operator.Divide.Help"), false)
            });

        [LocalizedDescription(typeof(AIKernelResource), "Parameter.Sample.Power")]//多选框
           [FunctionParameterUi(ParameterType.CheckBoxList, nameof(PowerOptions))]
           public string[] Power { get; set; }

           [JsonIgnore]
           public SelectionList PowerOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("2", AIKernelResource.Get("Parameter.Power.Square"), AIKernelResource.Get("Parameter.Power.Square.Help"), false),
                 new SelectionItem("3", AIKernelResource.Get("Parameter.Power.Cube"), AIKernelResource.Get("Parameter.Power.Cube.Help"), false)
            });
    }
}
