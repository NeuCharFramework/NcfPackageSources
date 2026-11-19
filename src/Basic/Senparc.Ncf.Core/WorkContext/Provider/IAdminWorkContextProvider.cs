/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IAdminWorkContextProvider.cs
    File description: IAdminWorkContextProvider related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

namespace Senparc.Ncf.Core.WorkContext.Provider
{
    public interface IAdminWorkContextProvider
    {
        AdminWorkContext GetAdminWorkContext();
    }
}