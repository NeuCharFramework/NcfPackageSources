## XncfBuild Prompt 备忘

### PromptResult 实体类

```bash
生成类PromptResult，包含属性：PromptGroupId（并添加PromptGroup类作为属性），LlmModelId（并添加LlmModel类作为属性），ResultString（结果字符串），CostTime（花费时间，单位：毫秒），RobotScore（机器人打分，0-100分），HumanScore（人类打分，0-100分），RobotTestExceptedResult，IsRobotTestExactlyEquat，TestType（测试类型，枚举中包含：文字、图形、声音）、PromptCostToken、ResultCostToken、TotalCostToken。请根据英文字面意思和括号内的说明生成对象并自动判断类型，同时加上可读性最高的注释。
```


## 文件说明

文件名 | 说明
---|---
Senparc.Xncf.PromptRange.csproj | NcfPackageSources 源文件项目，用于生成 Nuget 包。`依赖包直接引用源码项目。`
Senparc.Xncf.PromptRange.ForNcfEmbedding.csproj | 同样是源文件项目，用于放在 NCF 项目中直接用源码进行调试，也可以生成 Nuget 包。`依赖包使用 Nuget 包引用`。