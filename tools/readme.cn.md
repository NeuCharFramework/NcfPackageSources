# Tools 文件夹

文件/文件夹  |  说明
-------------|-------
[BuildXncfBuilderTemplate](BuildXncfBuilderTemplate)  |  本工具将对 ` Senparc.Xncf.XncfBuilder.Template` 项目中的文件进行替换操作，以便符合模板的占位符（参数）要求。
[NcfSimulatedSite](NcfSimulatedSite) | 用于模拟 NCF 模板运行的项目，此项目直接引用所有的 XNCF 木块源码进行测试，然后由 [NCF 模板项目](https://github.com/NeuCharFramework/NCF) 项目进行自动发布 [Senparc.NCF.Template Nuget 包](https://www.nuget.org/packages/Senparc.NCF.Template)。
[NcfSimulatedSiteTool](NcfSimulatedSiteTool) | 自动同 [NcfSimulatedSite](NcfSimulatedSite) 到 NCF 模板下面的快速文件对比和替换工具，要求 [NCF 模板项目](https://github.com/NeuCharFramework/NCF) 和当前项目的顶部文件夹并列放置在同一个上级文件夹内
remove-ncf-database.sql   |  彻底删除数据库脚本