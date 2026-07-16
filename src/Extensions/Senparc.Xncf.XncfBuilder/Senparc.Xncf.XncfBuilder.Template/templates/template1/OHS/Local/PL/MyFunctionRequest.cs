using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Ncf.Core.AppServices;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Template_OrgName.Xncf.Template_XncfName.OHS.Local.PL
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
}
