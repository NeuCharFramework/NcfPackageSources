# Tools 文件夹

文件/文件夹  |  说明
-------------|-------
[BuildXncfBuilderTemplate](BuildXncfBuilderTemplate)  |  本工具将对 ` Senparc.Xncf.XncfBuilder.Template` 项目中的文件进行替换操作，以便符合模板的占位符（参数）要求。
[NcfWebAdminBaseline](NcfWebAdminBaseline) | NCF Web Admin 标准基线，直接引用全部 XNCF 模块源码，用于集成开发、运行验证和模板同步；也是 [NCF 模板项目](https://github.com/NeuCharFramework/NCF) 后端内容的权威开发源之一，最终用于发布 [Senparc.NCF.Template NuGet 包](https://www.nuget.org/packages/Senparc.NCF.Template)。
[NcfWebAdminSyncTool](NcfWebAdminSyncTool) | 将 [NcfWebAdminBaseline](NcfWebAdminBaseline) 中的 Web Admin、模块源码和 XNCF 模板母版同步到对应打包项目；要求 [NCF 模板项目](https://github.com/NeuCharFramework/NCF) 和当前项目位于同一上级目录。
remove-ncf-database.sql   |  彻底删除数据库脚本
