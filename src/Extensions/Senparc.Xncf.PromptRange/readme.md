## XncfBuild Prompt 备忘

## Senparc.Xncf.PromptRange.csproj 文件说明

1. 当在 [NcfPackageSources](https://github.com/NeuCharFramework/NcfPackageSources/) 中引用时,`依赖包直接引用源码项目`，用于生成 Nuget 包和单元测试。
2. 当在	[NeuCharFarmework(NCF)](https://github.com/NeuCharFramework/NCF/) 中引用，并使用编译条件 `NcfDebugForPromptRange` 时，`依赖包使用 Nuget 包引用`，用于放在 NCF 项目中直接用源码进行调试。此时请将 NCF 和 NcfPackageSources 两个开源项目放在同一目录下。例如：

| 项目名称 | 项目路径 |
| :--- | :--- |
| NCF | D:\Senparc\NCF\NeuCharFarmework |
| NcfPackageSources | D:\Senparc\NCF\NcfPackageSources |

### PromptResult 实体类所使用的 Prompt

```bash
生成类PromptResult，包含属性：PromptGroupId（并添加PromptGroup类作为属性），LlmModelId（并添加LlmModel类作为属性），ResultString（结果字符串），CostTime（花费时间，单位：毫秒），RobotScore（机器人打分，0-100分），HumanScore（人类打分，0-100分），RobotTestExceptedResult，IsRobotTestExactlyEquat，TestType（测试类型，枚举中包含：文字、图形、声音）、PromptCostToken、ResultCostToken、TotalCostToken。请根据英文字面意思和括号内的说明生成对象并自动判断类型，同时加上可读性最高的注释。
```
