## XncfBuild Prompt Memo

## Senparc.Xncf.PromptRange.csproj file description

1. When referenced in [NcfPackageSources](https://github.com/NeuCharFramework/NcfPackageSources/), `dependency packages directly reference source code projects`, used to generate Nuget packages and unit tests.
2. When quoted in [NeuCharFarmework(NCF)](https://github.com/NeuCharFramework/NCF/), and use compilation conditions`NcfDebugForPromptRange`hour,`Dependent package uses Nuget package reference`, used to debug directly using the source code in the NCF project. At this time, please put the two open source projects NCF and NcfPackageSources in the same directory. For example:

| Project name | Project path |
| :--- | :--- |
| NCF | D:\Senparc\NCF\NeuCharFarmework |
| NcfPackageSources | D:\Senparc\NCF\NcfPackageSources |

### PromptResult Prompt used by entity classes

```bash
生成类PromptResult，包含属性：PromptGroupId（并添加PromptGroup类作为属性），LlmModelId（并添加LlmModel类作为属性），ResultString（结果字符串），CostTime（花费时间，单位：毫秒），RobotScore（机器人打分，0-100分），HumanScore（人类打分，0-100分），RobotTestExceptedResult，IsRobotTestExactlyEquat，TestType（测试类型，枚举中包含：文字、图形、声音）、PromptCostToken、ResultCostToken、TotalCostToken。请根据英文字面意思和括号内的说明生成对象并自动判断类型，同时加上可读性最高的注释。
```
