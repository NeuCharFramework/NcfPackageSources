/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IAdminWorkContextProvider.cs
    File description: IAdminWorkContextProvider related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

namespace Senparc.Ncf.Core.WorkContext.Provider
{
    public interface IAdminWorkContextProvider
    {
        AdminWorkContext GetAdminWorkContext();
    }
}