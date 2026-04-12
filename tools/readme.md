[中文版](readme.cn.md)

# Tools folder

File/Folder | Description
-------------|------
[BuildXncfBuilderTemplate](BuildXncfBuilderTemplate) | This tool will replace the files in the ` Senparc.Xncf.XncfBuilder.Template` project to comply with the placeholder (parameter) requirements of the template.
[NcfSimulatedSite](NcfSimulatedSite) | A project used to simulate the operation of NCF templates. This project directly references all XNCF block source codes for testing, and is then automatically released by the [NCF Template Project](https://github.com/NeuCharFramework/NCF) project [Senparc.NCF.Template Nuget Package](https://www.nuget.org/packages/Senparc.NCF.Template).
[NcfSimulatedSiteTool](NcfSimulatedSiteTool) | A quick file comparison and replacement tool that automatically compares [NcfSimulatedSite](NcfSimulatedSite) to the NCF template. It requires that the [NCF template project](https://github.com/NeuCharFramework/NCF) and the top folder of the current project are placed side by side in the same parent folder.
remove-ncf-database.sql | Completely delete the database script
