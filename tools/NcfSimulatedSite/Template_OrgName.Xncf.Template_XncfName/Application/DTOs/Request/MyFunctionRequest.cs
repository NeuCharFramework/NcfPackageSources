/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MyFunctionRequest.cs
    文件功能描述：MyFunctionRequest 相关实现
    
    
    创建标识：Senparc - 20211031
    
    修改标识：Senparc - 20260717
    修改描述：v1.1.0 更新示例 XNCF 模块的功能参数与页面本地化能力

    修改标识：Senparc - 20260724
    修改描述：v1.1.0 完善 XNCF 模板页面与资源的多语言支持

    修改标识：Senparc - 20260726
    修改描述：v1.1.0 补充示例模板 EventBus 请求-响应回环与多语言能力

----------------------------------------------------------------*/
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Ncf.Core.AppServices;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Template_OrgName.Xncf.Template_XncfName.Application.DTOs.Request
{
    public class MyFunction_CaculateRequest: FunctionAppRequestBase
    {
        [Required]
        [MaxLength(50)]
        [LocalizedDescription(typeof(Template_XncfNameResource), "Parameter.Sample.Name")]
        public string Name { get; set; }

        [Required]
        [LocalizedDescription(typeof(Template_XncfNameResource), "Parameter.Sample.Number1")]
        public int Number1 { get; set; }


        [Required]
        [LocalizedDescription(typeof(Template_XncfNameResource), "Parameter.Sample.Number2")]
        public int Number2 { get; set; }

        [LocalizedDescription(typeof(Template_XncfNameResource), "Parameter.Sample.Operator")]//下拉列表
           [FunctionParameterUi(ParameterType.DropDownList, nameof(OperatorOptions))]
           public string Operator { get; set; }

           [JsonIgnore]
           public SelectionList OperatorOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new[] {
                 new SelectionItem("+", Template_XncfNameResource.Get("Parameter.Operator.Add"), Template_XncfNameResource.Get("Parameter.Operator.Add.Help"), false),
                 new SelectionItem("-", Template_XncfNameResource.Get("Parameter.Operator.Subtract"), Template_XncfNameResource.Get("Parameter.Operator.Subtract.Help"), true),
                 new SelectionItem("×", Template_XncfNameResource.Get("Parameter.Operator.Multiply"), Template_XncfNameResource.Get("Parameter.Operator.Multiply.Help"), false),
                 new SelectionItem("÷", Template_XncfNameResource.Get("Parameter.Operator.Divide"), Template_XncfNameResource.Get("Parameter.Operator.Divide.Help"), false)
            });

        [LocalizedDescription(typeof(Template_XncfNameResource), "Parameter.Sample.Power")]//多选框
           [FunctionParameterUi(ParameterType.CheckBoxList, nameof(PowerOptions))]
           public string[] Power { get; set; }

           [JsonIgnore]
           public SelectionList PowerOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("2", Template_XncfNameResource.Get("Parameter.Power.Square"), Template_XncfNameResource.Get("Parameter.Power.Square.Help"), false),
                 new SelectionItem("3", Template_XncfNameResource.Get("Parameter.Power.Cube"), Template_XncfNameResource.Get("Parameter.Power.Cube.Help"), false)
            });
    }

    /// <summary>
    /// EventBus 内部回环健康检查不接收业务参数，避免把测试入口变成数据访问接口。
    /// </summary>
    public class EventBusRoundTripRequest : FunctionAppRequestBase
    {
    }
}
