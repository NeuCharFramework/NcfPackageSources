
        namespace Senparc.Xncf.XncfBuilder.OHS.Local
        { 
            public partial class BuildXncfAppService
            {
                public const string BackendTemplate = @"
        ## Database EntityFramework DbContext class sample
        File Name: Template_XncfNameSenparcEntities.cs
        File Path: <ModuleRootPath>/Domain/Models/DatabaseModel
        Code:
        ```csharp
        
        ```

        ## Database Entity class sample
        File Name: Color.cs
        File Path: <ModuleRootPath>/Domain/Models/DatabaseModel
        Code:
        ```csharp
        
        ```

        ## Database Entity DTO class sample
        File Name: ColorDto.cs
        File Path: <ModuleRootPath>/Domain/Models/DatabaseModel/Dto
        Code:
        ```csharp
        
        ```

        ## Service class sample
        File Name: Template_XncfNameService.cs
        File Path: <ModuleRootPath>/Domain/Services
        Code:
        ```csharp
        
        ```
        ";
                public const string FrontendTemplate = @"
        ## Page UI sample (front-end)
        File Name: DatabaseSampleIndex.cshtml
        File Path: <ModuleRootPath>/Areas/Admin/Pages/Template_XncfName
        Code:
        ```razorpage
        
        ```

        ## Page UI sample (back-end)
        File Name: DatabaseSampleIndex.cshtml.cs
        File Path: <ModuleRootPath>/Areas/Admin/Pages/Template_XncfName
        Code:
        ```csharp
        
        ```

        ## Page JavaScript file sample
        File Name: databaseSampleIndex.js
        File Path: <ModuleRootPath>/wwwroot/js/Admin/Template_XncfName
        Code:
        ```javascript
        
        ```

        ## Page CSS file sample
        File Name: databaseSampleIndex.css
        File Path: <ModuleRootPath>/wwwroot/css/Admin/Template_XncfName
        Code:
        ```css
        
        ```
        ";
            }
        }