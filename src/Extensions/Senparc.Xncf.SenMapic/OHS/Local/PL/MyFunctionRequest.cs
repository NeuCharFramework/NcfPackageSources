/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MyFunctionRequest.cs
    文件功能描述：MyFunctionRequest 相关实现
    
    
    创建标识：Senparc - 20250113
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Ncf.XncfBase;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Senparc.Xncf.SenMapic.OHS.Local.PL
{
    public class MyFunction_SenMapicRequest : FunctionAppRequestBase
    {
        [Required]
        [LocalizedDescription(typeof(SenMapicResource), "Parameter.SenMapic.Url")]
        public string Url { get; set; }

        [Required]
        [LocalizedDescription(typeof(SenMapicResource), "Parameter.SenMapic.Depth")]
        public int Deepth { get; set; }

        [Required]
        [LocalizedDescription(typeof(SenMapicResource), "Parameter.SenMapic.PageCount")]
        public int PageNumber { get; set; }

    }
    public class MyFunction_CaculateRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(50)]
        [LocalizedDescription(typeof(SenMapicResource), "Parameter.Sample.Name")]
        public string Name { get; set; }

        [Required]
        [LocalizedDescription(typeof(SenMapicResource), "Parameter.Sample.Number1")]
        public int Number1 { get; set; }


        [Required]
        [LocalizedDescription(typeof(SenMapicResource), "Parameter.Sample.Number2")]
        public int Number2 { get; set; }

        [LocalizedDescription(typeof(SenMapicResource), "Parameter.Sample.OperatorRestricted")]//下拉列表
        [FunctionParameterUi(ParameterType.DropDownList, nameof(OperatorOptions))]
        public string Operator { get; set; }

        [JsonIgnore]
        public SelectionList OperatorOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new[] {
                 new SelectionItem("+", SenMapicResource.Get("Parameter.Operator.Add"), SenMapicResource.Get("Parameter.Operator.Add.Help"), true),
                 new SelectionItem("-", SenMapicResource.Get("Parameter.Operator.Subtract"), SenMapicResource.Get("Parameter.Operator.Subtract.Help"), false),
                 new SelectionItem("×", SenMapicResource.Get("Parameter.Operator.Multiply"), SenMapicResource.Get("Parameter.Operator.Multiply.Help"), false),
                 new SelectionItem("÷", SenMapicResource.Get("Parameter.Operator.Divide"), SenMapicResource.Get("Parameter.Operator.Divide.Help"), false)
            });

        [LocalizedDescription(typeof(SenMapicResource), "Parameter.Sample.PowerRestricted")]//多选框
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(PowerOptions))]
        public string[] Power { get; set; }

        [JsonIgnore]
        public SelectionList PowerOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("2", SenMapicResource.Get("Parameter.Power.Square"), SenMapicResource.Get("Parameter.Power.Square.Help"), false),
                 new SelectionItem("3", SenMapicResource.Get("Parameter.Power.Cube"), SenMapicResource.Get("Parameter.Power.Cube.Help"), false)
            });
    }
}
