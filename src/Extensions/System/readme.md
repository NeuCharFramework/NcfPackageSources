# System 文件夹说明

此文件夹中都是系统指定的 XNCF 顶级模块。

## 须知

`Senparc.Xncf.SystemCore` 是所有模块启动所必须的前置模块，所以每个项目必须添加（特别是数据库项目）。

> 后期会考虑集成到一个专门的 DatabaseCore 项目中。

## 数字前缀

命名前缀中的数字（如 `[5999]`）表示该模块在启动过程中的执行优先级，数字越大，执行越靠前。